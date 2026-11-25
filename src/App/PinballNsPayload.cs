using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

// Autor: Miquel Garrigo
internal sealed class PinballNsPayload
{
    internal const string Version = "2.9";
    internal const string ExecutableName = "PinballNS.exe";
    private const int ArchiveBytes = 264649;
    private const int MaxRomBytes = 8 * 1024 * 1024;
    private const string ArchiveHash = "0ed6770697229eb177e348d1567dde9127f2ab36a1dfdb312ba6314018a7e67a";
    private const string ExecutableHash = "067eb1938a87d177dfd9615da019d5e395e9328085b94dfcf9cfb0167a0a505e";
    private const string OriginalRomHash = "9402014d14969432142abfde728c6f1a10ee4dac";
    private const string GenerationsRomHash = "9407074cc9b5a93d731a07e2d24b9ca06ecf17d7";
    private static readonly Uri DownloadUrl = new Uri(
        "https://github.com/ReiniShari/Pinball-NS/releases/download/v" + Version + "/PinballNS-Windows-" + Version + ".zip");
    private static readonly SemaphoreSlim SetupGate = new SemaphoreSlim(1, 1);
    private readonly string executable, rom;

    private PinballNsPayload(string engine, string path) { executable = engine; rom = path; }

    internal ProcessStartInfo StartInfo()
    {
        return new ProcessStartInfo(executable, "\"" + rom + "\"")
        {
            UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = Path.GetDirectoryName(executable)
        };
    }

    internal static void ValidateRom(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            throw new IOException("Elige el archivo del juego en Configurar → Tema → Pokémon.");
        using (var input = File.OpenRead(path))
        using (var sha = SHA1.Create())
        {
            if (input.Length > 0 && input.Length <= MaxRomBytes)
            {
                string hash = BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
                if (hash == OriginalRomHash || hash == GenerationsRomHash) return;
            }
        }
        throw new IOException("El archivo no es compatible. Elige tu ROM USA/Australia con vibración o Pokémon Pinball Generations 1.6.");
    }

    internal static async Task<PinballNsPayload> PrepareAsync(string path, CancellationToken cancellation)
    {
        ValidateRom(path);
        if (!Environment.Is64BitOperatingSystem)
            throw new IOException("Pinball NS requiere Windows de 64 bits.");

        await SetupGate.WaitAsync(cancellation).ConfigureAwait(false);
        try
        {
            string root = CheckedDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DrifftAnalyzer-PinballNS"));
            string directory = CheckedDirectory(Path.Combine(root, Version));
            string executable = Path.Combine(directory, ExecutableName);
            if (!PinballPayload.Matches(executable, ExecutableHash))
            {
                using (var zip = await DownloadAsync(cancellation).ConfigureAwait(false))
                    Unpack(zip, executable);
            }
            cancellation.ThrowIfCancellationRequested();
            Mute(Path.Combine(directory, "pinball_ns.cfg"));
            return new PinballNsPayload(executable, Path.GetFullPath(path));
        }
        finally { SetupGate.Release(); }
    }

    private static void Mute(string path)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, "sound=0\r\n");
            return;
        }
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Los ajustes de Pinball NS no pueden ser un enlace.");
        string[] lines = File.ReadAllLines(path);
        for (int i = 0; i < lines.Length; i++)
        {
            if (!lines[i].StartsWith("sound=", StringComparison.Ordinal)) continue;
            if (lines[i] != "sound=0")
            {
                lines[i] = "sound=0";
                File.WriteAllLines(path, lines);
            }
            return;
        }
        File.AppendAllText(path, "\r\nsound=0\r\n");
    }

    private static string CheckedDirectory(string path)
    {
        Directory.CreateDirectory(path);
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("La caché del juego no puede ser un enlace.");
        return path;
    }

    internal static void Unpack(Stream zip, string executable)
    {
        if (zip.Length != ArchiveBytes || PinballPayload.Hash(zip) != ArchiveHash)
            throw new IOException("La descarga de Pinball NS no supera la comprobación de integridad.");
        zip.Position = 0;
        using (var archive = new ZipArchive(zip, ZipArchiveMode.Read, true))
        using (var input = archive.GetEntry(ExecutableName).Open())
            PinballPayload.Save(executable, input, ExecutableHash);
    }

    internal static async Task<MemoryStream> DownloadAsync(CancellationToken cancellation)
    {
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        using (var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }))
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("DrifftAnalyzer/" + ProductVersion.Current);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            try
            {
                using (var response = await UpdateTransport.OpenAsync(client, DownloadUrl, timeout.Token).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        var zip = new MemoryStream(ArchiveBytes);
                        try
                        {
                            var buffer = new byte[8192];
                            int count;
                            while ((count = await input.ReadAsync(buffer, 0, buffer.Length, timeout.Token).ConfigureAwait(false)) > 0)
                            {
                                if (zip.Length + count > ArchiveBytes) throw new IOException("La descarga de Pinball NS tiene un tamaño inesperado.");
                                zip.Write(buffer, 0, count);
                            }
                            zip.Position = 0;
                            return zip;
                        }
                        catch { zip.Dispose(); throw; }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                cancellation.ThrowIfCancellationRequested();
                throw new IOException("Se agotó el tiempo de descarga de Pinball NS. Vuelve a intentarlo.");
            }
            catch (HttpRequestException ex)
            {
                throw new IOException("No se pudo descargar Pinball NS de GitHub. Comprueba la conexión y vuelve a intentarlo.", ex);
            }
        }
    }
}
