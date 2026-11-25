using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

// Autor: Miquel Garrigo
internal sealed class UpdateException : Exception
{
    internal UpdateException(string message) : base(message) { }
}

public sealed class UpdateRelease
{
    public string Tag { get; set; }
    public string Version { get; set; }
    public string DownloadUrl { get; set; }
    public string Sha256 { get; set; }
    public long Size { get; set; }
    public string AssetName { get; set; }
    internal string FileName { get { return string.IsNullOrEmpty(AssetName) ? ReleaseSource.InstallerName : AssetName; } }
    internal const long MaximumSize = 256L * 1024 * 1024;

    internal static System.Version ParseVersion(string value)
    {
        if (string.IsNullOrEmpty(value) || !Regex.IsMatch(value, @"^v?(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$"))
            throw new UpdateException("La versión publicada tiene un formato que esta aplicación no reconoce.");
        System.Version parsed;
        if (!System.Version.TryParse(value.TrimStart('v'), out parsed))
            throw new UpdateException("No se pudo leer la versión publicada.");
        return new System.Version(parsed.Major, parsed.Minor, parsed.Build, 0);
    }

    internal void Validate()
    {
        if (ParseVersion(Tag) != ParseVersion(Version)) throw new UpdateException("Los datos de la versión no coinciden.");
        if (FileName != ReleaseSource.InstallerName && FileName != ReleaseSource.FullInstallerName)
            throw new UpdateException("El nombre del instalador no es válido.");
        string expected = ReleaseSource.RepositoryUrl + "/releases/download/" + Uri.EscapeDataString(Tag) + "/" + FileName;
        Uri uri;
        if (!Uri.TryCreate(DownloadUrl, UriKind.Absolute, out uri) || uri.Scheme != "https" ||
            !string.Equals(DownloadUrl, expected, StringComparison.Ordinal) || !string.IsNullOrEmpty(uri.UserInfo))
            throw new UpdateException("La descarga no pertenece al repositorio de la aplicación.");
        if (Size <= 0 || Size > MaximumSize || Sha256 == null || !Regex.IsMatch(Sha256, "^[a-fA-F0-9]{64}$"))
            throw new UpdateException("La actualización aún no tiene una descarga verificable.");
    }

    internal static UpdateRelease FromJson(string json, string currentVersion)
    {
        GitHubRelease release;
        using (var input = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            release = (GitHubRelease)new DataContractJsonSerializer(typeof(GitHubRelease)).ReadObject(input);
        if (release == null || release.Draft || release.Prerelease)
            throw new UpdateException("La versión publicada todavía no es estable.");
        var version = ParseVersion(release.Tag);
        if (version <= ParseVersion(currentVersion)) return null;
        if (release.Assets != null)
            foreach (string name in new[] { ReleaseSource.FullInstallerName, ReleaseSource.InstallerName })
            foreach (var asset in release.Assets)
                if (asset.Name == name && asset.State == "uploaded")
                {
                    var result = new UpdateRelease
                    {
                        Tag = release.Tag, Version = version.ToString(3), DownloadUrl = asset.Url,
                        Sha256 = asset.Digest != null && asset.Digest.StartsWith("sha256:", StringComparison.Ordinal) ? asset.Digest.Substring(7) : null,
                        Size = asset.Size, AssetName = name
                    };
                    result.Validate();
                    return result;
                }
        throw new UpdateException("La nueva versión aún no tiene instalador disponible.");
    }
}

[DataContract]
internal sealed class GitHubRelease
{
    [DataMember(Name = "tag_name", IsRequired = true)] internal string Tag { get; set; }
    [DataMember(Name = "draft", IsRequired = true)] internal bool Draft { get; set; }
    [DataMember(Name = "prerelease", IsRequired = true)] internal bool Prerelease { get; set; }
    [DataMember(Name = "assets", IsRequired = true)] internal GitHubAsset[] Assets { get; set; }
}

[DataContract]
internal sealed class GitHubAsset
{
    [DataMember(Name = "name")] internal string Name { get; set; }
    [DataMember(Name = "state")] internal string State { get; set; }
    [DataMember(Name = "browser_download_url")] internal string Url { get; set; }
    [DataMember(Name = "digest")] internal string Digest { get; set; }
    [DataMember(Name = "size")] internal long Size { get; set; }
}

internal interface IReleaseFeed : IDisposable
{
    Task<UpdateRelease> LatestAsync(string currentVersion, CancellationToken cancellation);
}

internal interface IInstallerDownload : IDisposable
{
    Task<string> DownloadAsync(UpdateRelease release, CancellationToken cancellation);
}

internal sealed class GitHubReleaseFeed : IReleaseFeed
{
    private readonly HttpClient client;
    internal GitHubReleaseFeed() : this(new HttpClientHandler { AllowAutoRedirect = false }) { }
    internal GitHubReleaseFeed(HttpMessageHandler handler)
    {
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) };
        client.MaxResponseContentBufferSize = 1024 * 1024;
        client.DefaultRequestHeaders.UserAgent.ParseAdd("DrifftAnalyzer/" + ProductVersion.Current);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    }

    public async Task<UpdateRelease> LatestAsync(string currentVersion, CancellationToken cancellation)
    {
        try { return await ReadAsync(ReleaseSource.LatestApi, currentVersion, cancellation).ConfigureAwait(false); }
        catch (Exception ex)
        {
            if (cancellation.IsCancellationRequested) throw;
            UpdateLog.Write("buscar-api", ex);
        }
        // El manifiesto de la misma release permite actualizar sin depender de la API y sus cuotas.
        try { return await ReadAsync(ReleaseSource.LatestManifest, currentVersion, cancellation).ConfigureAwait(false); }
        catch (Exception ex)
        {
            if (cancellation.IsCancellationRequested) throw;
            UpdateLog.Write("buscar-manifiesto", ex);
            throw new UpdateException("No se pudo consultar GitHub. La app sigue disponible; puedes reintentar o descargar Iniciar.exe desde la página de versiones.");
        }
    }
    private async Task<UpdateRelease> ReadAsync(string url, string currentVersion, CancellationToken cancellation)
    {
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
        {
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            using (var response = await UpdateTransport.OpenAsync(client, new Uri(url), timeout.Token).ConfigureAwait(false))
            {
            response.EnsureSuccessStatusCode();
                using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var body = new MemoryStream())
                using (timeout.Token.Register(delegate { input.Dispose(); }))
                {
                    var buffer = new byte[8192]; int count;
                    while ((count = await input.ReadAsync(buffer, 0, buffer.Length, timeout.Token).ConfigureAwait(false)) > 0)
                    {
                        if (body.Length + count > 1024 * 1024) throw new UpdateException("La respuesta de actualizaciones no es válida.");
                        body.Write(buffer, 0, count);
                    }
                    return UpdateRelease.FromJson(Encoding.UTF8.GetString(body.ToArray()), currentVersion);
                }
            }
        }
    }
    public void Dispose() { client.Dispose(); }
}

internal sealed class InstallerDownload : IInstallerDownload
{
    private readonly HttpClient client;
    private readonly string temporaryRoot;
    internal InstallerDownload() : this(new HttpClientHandler { AllowAutoRedirect = false }, Path.GetTempPath()) { }
    internal InstallerDownload(HttpMessageHandler handler, string tempRoot)
    {
        // También se usa desde Iniciar.exe sin haber creado antes el cliente de la API.
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(4) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("DrifftAnalyzer/" + ProductVersion.Current);
        temporaryRoot = Path.GetFullPath(tempRoot);
    }

    public async Task<string> DownloadAsync(UpdateRelease release, CancellationToken cancellation)
    {
        release.Validate();
        string directory = Path.Combine(temporaryRoot, "DrifftAnalyzer-Update-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string file = Path.Combine(directory, release.FileName);
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
        {
        timeout.CancelAfter(TimeSpan.FromMinutes(10));
        cancellation = timeout.Token;
        try
        {
            using (var response = await UpdateTransport.OpenAsync(client, new Uri(release.DownloadUrl), cancellation).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentLength.HasValue && response.Content.Headers.ContentLength.Value != release.Size)
                    throw new UpdateException("El tamaño de la descarga no coincide con la versión publicada.");
                using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var output = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None, 8192, true))
                using (cancellation.Register(delegate { input.Dispose(); }))
                {
                    byte[] buffer = new byte[8192];
                    long total = 0;
                    int count;
                    while ((count = await input.ReadAsync(buffer, 0, buffer.Length, cancellation).ConfigureAwait(false)) > 0)
                    {
                        total += count;
                        if (total > release.Size || total > UpdateRelease.MaximumSize) throw new UpdateException("La descarga supera el tamaño publicado.");
                        await output.WriteAsync(buffer, 0, count, cancellation).ConfigureAwait(false);
                    }
                    if (total != release.Size) throw new UpdateException("La descarga está incompleta.");
                }
            }
            string actual;
            using (var input = File.OpenRead(file))
            using (var sha = SHA256.Create()) actual = BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "");
            if (!string.Equals(actual, release.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new UpdateException("La descarga no supera la comprobación de integridad. No se instalará.");
            using (var input = File.OpenRead(file))
                if (input.ReadByte() != 'M' || input.ReadByte() != 'Z') throw new UpdateException("El archivo descargado no es un instalador de Windows.");
            return file;
        }
        catch
        {
            try
            {
                if (File.Exists(file)) File.Delete(file);
                if (Directory.Exists(directory) && Directory.GetFileSystemEntries(directory).Length == 0) Directory.Delete(directory, false);
            }
            catch (Exception ex) { UpdateLog.Write("limpiar-descarga", ex); }
            throw;
        }
        }
    }
    public void Dispose() { client.Dispose(); }
}
