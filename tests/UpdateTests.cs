using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

internal sealed class StubHandler : HttpMessageHandler
{
    internal Func<HttpRequestMessage, HttpResponseMessage> Respond;
    internal int Calls;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        Calls++;
        if (request.Headers.Authorization != null) throw new Exception("La consulta pública no debe enviar credenciales.");
        return Task.FromResult(Respond(request));
    }
}
internal sealed class StubFeed : IReleaseFeed
{
    internal UpdateRelease Release;
    internal Exception Error;
    internal int Calls;
    internal TaskCompletionSource<UpdateRelease> Pending;
    public Task<UpdateRelease> LatestAsync(string current, CancellationToken token)
    {
        Calls++;
        if (Pending != null) { token.Register(delegate { Pending.TrySetCanceled(); }); return Pending.Task; }
        if (Error != null) { var failure = new TaskCompletionSource<UpdateRelease>(); failure.SetException(Error); return failure.Task; }
        return Task.FromResult(Release);
    }
    public void Dispose() { }
}
internal sealed class StubDownload : IInstallerDownload
{
    internal int Calls;
    internal bool Fail;
    public Task<string> DownloadAsync(UpdateRelease release, CancellationToken token)
    {
        Calls++;
        if (Fail) throw new UpdateException("Descarga rechazada para la prueba.");
        return Task.FromResult("instalador-verificado.exe");
    }
    public void Dispose() { }
}

internal sealed class WaitingBody : Stream
{
    public override bool CanRead { get { return true; } }
    public override bool CanSeek { get { return false; } }
    public override bool CanWrite { get { return false; } }
    public override long Length { get { throw new NotSupportedException(); } }
    public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellation)
    {
        var pending = new TaskCompletionSource<int>();
        cancellation.Register(delegate { pending.TrySetCanceled(); });
        return pending.Task;
    }
    public override int Read(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
    public override void SetLength(long value) { throw new NotSupportedException(); }
    public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
}

internal static class UpdateTests
{
    private static int count;
    private static string root;
    private static readonly byte[] installer = Encoding.ASCII.GetBytes("MZ instalador de prueba; nunca se ejecuta");
    internal static string Hash(byte[] bytes)
    {
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }
    internal static UpdateRelease NewRelease()
    {
        return new UpdateRelease { Tag = "v1.2.0", Version = "1.2.0", DownloadUrl = ReleaseSource.RepositoryUrl + "/releases/download/v1.2.0/Iniciar.exe", Sha256 = Hash(installer), Size = installer.Length };
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        count++;
        Console.WriteLine("OK " + message);
    }
    private static void Reject(Action action, string message)
    {
        try { action(); } catch { Check(true, message); return; }
        throw new Exception("No se rechazó: " + message);
    }
    private static string Cache() { return Path.Combine(root, Guid.NewGuid().ToString("N").Substring(0, 8) + ".xml"); }
    private static string Json(string tag, bool draft, bool preview, string url, string digest)
    {
        return "{\"tag_name\":\"" + tag + "\",\"draft\":" + draft.ToString().ToLowerInvariant() + ",\"prerelease\":" + preview.ToString().ToLowerInvariant() +
            ",\"assets\":[{\"name\":\"Iniciar.exe\",\"state\":\"uploaded\",\"browser_download_url\":\"" + url + "\",\"digest\":\"" + digest + "\",\"size\":" + installer.Length + "}]}";
    }
    private static UpdateManager Manager(StubFeed feed, StubDownload download, Action<string,string> launch, string cache, Func<DateTime> clock)
    {
        return new UpdateManager(feed, download, launch, cache, Path.Combine(root, "ajustes.xml"), "1.1.0", clock);
    }

    private static int Main()
    {
        try
        {
            root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updates-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(root);
            Run().GetAwaiter().GetResult();
            Console.WriteLine("Superadas " + count + " comprobaciones de actualizaciones.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { if (root != null && Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static async Task Run()
    {
        var previousProtocol = ServicePointManager.SecurityProtocol;
        try
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
            using (var freshDownload = new InstallerDownload(new StubHandler { Respond = delegate { return new HttpResponseMessage(HttpStatusCode.OK); } }, root))
                Check((ServicePointManager.SecurityProtocol & SecurityProtocolType.Tls12) != 0, "El instalador inicial activa TLS 1.2 sin depender de haber consultado antes la API");
        }
        finally { ServicePointManager.SecurityProtocol = previousProtocol; }
        var release = NewRelease();
        string json = Json(release.Tag, false, false, release.DownloadUrl, "sha256:" + release.Sha256);
        Check(UpdateRelease.ParseVersion("v1.10.0") > UpdateRelease.ParseVersion("1.9.0"), "Compara versiones numéricamente");
        Check(UpdateRelease.FromJson(json, "1.1.0").Version == "1.2.0", "Reconoce la versión estable nueva con su SHA-256");
        Check(UpdateRelease.FromJson(json, "1.2.0") == null && UpdateRelease.FromJson(json, "1.3.0") == null, "No ofrece reinstalar ni bajar de versión");
        var large = NewRelease(); large.Size = 110L * 1024 * 1024; large.Validate();
        Check(true, "Admite el tamaño real del instalador con el juego completo, superior a 64 MiB");
        large.Size = UpdateRelease.MaximumSize + 1;
        Reject(delegate { large.Validate(); }, "Mantiene un límite de tamaño para rechazar descargas desmesuradas");
        string fullJson = json.Replace("Iniciar.exe", ReleaseSource.FullInstallerName);
        Check(UpdateRelease.FromJson(fullJson, "1.1.0").FileName == ReleaseSource.FullInstallerName, "Las versiones nuevas reconocen el instalador completo");
        string combinedJson = json.Substring(0, json.Length - 2) + "," + fullJson.Substring(fullJson.IndexOf('[') + 1);
        Check(UpdateRelease.FromJson(combinedJson, "1.1.0").FileName == ReleaseSource.FullInstallerName, "El cliente nuevo prefiere el paquete completo aunque el instalador antiguo aparezca primero");
        Reject(delegate { UpdateRelease.ParseVersion("v1.2.0-beta"); }, "Rechaza etiquetas no estables");
        Reject(delegate { UpdateRelease.FromJson(Json(release.Tag, true, false, release.DownloadUrl, "sha256:" + release.Sha256), "1.1.0"); }, "Rechaza borradores");
        Reject(delegate { UpdateRelease.FromJson(Json(release.Tag, false, true, release.DownloadUrl, "sha256:" + release.Sha256), "1.1.0"); }, "Rechaza versiones preliminares");
        Reject(delegate { UpdateRelease.FromJson(Json(release.Tag, false, false, "https://otro.example/Iniciar.exe", "sha256:" + release.Sha256), "1.1.0"); }, "Rechaza instaladores de otro origen");
        Reject(delegate { UpdateRelease.FromJson(Json(release.Tag, false, false, release.DownloadUrl, ""), "1.1.0"); }, "No ofrece instaladores sin integridad verificable");
        var handler = new StubHandler { Respond = delegate(HttpRequestMessage request)
            { Check(request.RequestUri.AbsoluteUri == ReleaseSource.LatestApi, "Consulta exclusivamente la release pública de la app"); return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) }; } };
        using (var feed = new GitHubReleaseFeed(handler)) Check((await feed.LatestAsync("1.1.0", CancellationToken.None)).Version == "1.2.0", "Lee la respuesta HTTP real del cliente");
        using (var cancel = new CancellationTokenSource())
        using (var stalled = new InstallerDownload(new StubHandler { Respond = delegate { return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new WaitingBody()) }; } }, Path.Combine(root, "descarga parada")))
        {
            var pending = stalled.DownloadAsync(release, cancel.Token);
            cancel.CancelAfter(50);
            Check(await Task.WhenAny(pending, Task.Delay(5000)) == pending, "Una descarga cuyo cuerpo deja de responder se puede cancelar sin bloquear la app");
            try { await pending; throw new Exception("La descarga debía cancelarse."); } catch (OperationCanceledException) { }
            Check(Directory.GetFiles(Path.Combine(root, "descarga parada"), "*.exe", SearchOption.AllDirectories).Length == 0,
                "Cancelar elimina el archivo incompleto y nunca lo instala");
        }
        foreach (var status in new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden })
        {
            using (var feed = new GitHubReleaseFeed(new StubHandler { Respond = delegate { return new HttpResponseMessage(status); } }))
                Reject(delegate { feed.LatestAsync("1.1.0", CancellationToken.None).GetAwaiter().GetResult(); }, "El error HTTP " + (int)status + " no se confunde con estar actualizado");
        }

        var fallback = new StubHandler { Respond = delegate(HttpRequestMessage request)
        {
            if (request.RequestUri.AbsoluteUri == ReleaseSource.LatestApi) return new HttpResponseMessage(HttpStatusCode.Forbidden);
            Check(request.RequestUri.AbsoluteUri == ReleaseSource.LatestManifest, "La alternativa a la API pertenece a la misma release oficial");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(fullJson) };
        } };
        using (var feed = new GitHubReleaseFeed(fallback))
            Check((await feed.LatestAsync("1.1.0", CancellationToken.None)).FileName == ReleaseSource.FullInstallerName && fallback.Calls == 2,
                "Si la API limita consultas, el manifiesto permite seguir actualizando con integridad verificada");
        var transient = new StubHandler();
        transient.Respond = delegate { return transient.Calls < 3 ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) }; };
        using (var feed = new GitHubReleaseFeed(transient))
            Check((await feed.LatestAsync("1.1.0", CancellationToken.None)).Version == "1.2.0" && transient.Calls == 3,
                "Reintenta errores temporales de GitHub sin cerrar la app");

        string downloads = Path.Combine(root, "descargas válidas"); Directory.CreateDirectory(downloads);
        var redirect = new StubHandler { Respond = delegate(HttpRequestMessage request)
        {
            if (request.RequestUri.Host == "github.com")
            {
                var response = new HttpResponseMessage(HttpStatusCode.Redirect);
                response.Headers.Location = new Uri("https://release-assets.githubusercontent.com/test/installer");
                return response;
            }
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(installer) };
        } };
        using (var download = new InstallerDownload(redirect, downloads))
        {
            string file = await download.DownloadAsync(release, CancellationToken.None);
            Check(File.Exists(file) && Hash(File.ReadAllBytes(file)) == release.Sha256 && redirect.Calls == 2, "Descarga mediante CDN de GitHub y comprueba el archivo completo");
        }
        foreach (bool wrongHash in new[] { true, false })
        {
            var bad = NewRelease();
            if (wrongHash) bad.Sha256 = new string('0', 64); else bad.Size++;
            string rejected = Path.Combine(root, "rechazada-" + wrongHash); Directory.CreateDirectory(rejected);
            using (var download = new InstallerDownload(new StubHandler { Respond = delegate { return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(installer) }; } }, rejected))
                Reject(delegate { download.DownloadAsync(bad, CancellationToken.None).GetAwaiter().GetResult(); }, wrongHash ? "Rechaza un SHA-256 distinto" : "Rechaza una descarga de tamaño incorrecto");
            Check(Directory.GetFileSystemEntries(rejected).Length == 0, "Elimina archivos de una descarga rechazada");
        }
        foreach (string location in new[] { "https://otro.example/app.exe", "http://github.com/app.exe" })
        {
            var badRedirect = new StubHandler { Respond = delegate
            {
                var response = new HttpResponseMessage(HttpStatusCode.Redirect); response.Headers.Location = new Uri(location); return response;
            } };
            using (var download = new InstallerDownload(badRedirect, root))
                Reject(delegate { download.DownloadAsync(release, CancellationToken.None).GetAwaiter().GetResult(); }, "No sigue redirecciones fuera de HTTPS/GitHub");
            Check(badRedirect.Calls == 1, "No realiza la petición a la redirección rechazada");
        }

        var now = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
        var currentFeed = new StubFeed();
        string cache = Cache();
        using (var manager = Manager(currentFeed, new StubDownload(), delegate { throw new Exception("No debe instalar."); }, cache, delegate { return now; }))
        {
            Check(manager.IsDue, "La primera búsqueda automática está pendiente");
            await manager.CheckAsync();
            Check(manager.Phase == UpdatePhase.Current && manager.Message.Contains("última versión (1.1.0)") && !manager.IsBusy, "La búsqueda manual indica claramente que ya está en la última versión");
            Check(!manager.IsDue, "No repite búsquedas automáticas antes de 12 horas");
            await manager.CheckAsync();
            Check(currentFeed.Calls == 2, "La búsqueda manual funciona aunque aún no toque la automática");
            now = now.AddHours(12);
            Check(manager.IsDue, "Vuelve a buscar al cumplirse 12 horas");
        }
        using (var manager = Manager(new StubFeed(), new StubDownload(), delegate { }, Cache(), delegate { return now; }))
        {
            int notifications = 0;
            manager.Changed += delegate { throw new InvalidOperationException("Vista ya cerrada para la prueba"); };
            manager.Changed += delegate { notifications++; };
            await manager.CheckAsync();
            Check(manager.Phase == UpdatePhase.Current && !manager.IsBusy && notifications >= 2,
                "Un fallo al refrescar una vista no cierra la app ni impide actualizar las demás vistas");
        }
        Func<string, string, Task> failedInstaller = async delegate { await Task.Delay(10); throw new IOException("Instalador rechazado para la prueba"); };
        using (var manager = new UpdateManager(new StubFeed { Release = release }, new StubDownload(), failedInstaller, Cache(), Path.Combine(root, "ajustes.xml"), "1.1.0", delegate { return now; }))
        {
            await manager.InstallAsync();
            Check(manager.Phase == UpdatePhase.Error && !manager.IsBusy && manager.Available != null,
                "Si el instalador falla después de arrancar, el botón permite reintentar y la app continúa disponible");
        }
        using (var restored = Manager(new StubFeed(), new StubDownload(), delegate { }, cache, delegate { return now.AddHours(-11); }))
            Check(!restored.IsDue, "Recuerda la última comprobación al reiniciar");

        var availableFeed = new StubFeed { Release = release };
        var installerDownload = new StubDownload();
        int launches = 0; string preferences = null;
        string pendingCache = Cache();
        using (var manager = Manager(availableFeed, installerDownload, delegate(string file, string settings) { launches++; preferences = settings; }, pendingCache, delegate { return now; }))
        {
            await manager.CheckAsync();
            Check(manager.Available != null && manager.Phase == UpdatePhase.Available && !manager.IsBusy, "Habilita el estado de actualización pendiente sin instalar automáticamente");
            Check(launches == 0 && installerDownload.Calls == 0, "Detectar una actualización no descarga ni ejecuta nada");
            await manager.InstallAsync();
            Check(availableFeed.Calls == 2 && launches == 1 && installerDownload.Calls == 1 && preferences.EndsWith("ajustes.xml"), "Al actualizar revalida la release y entrega los ajustes al instalador");
            Check(manager.Phase == UpdatePhase.Installing && manager.IsBusy, "Evita iniciar una segunda instalación mientras se reinicia");
        }
        var offline = new StubFeed { Error = new HttpRequestException("Sin red") };
        var noDownload = new StubDownload();
        using (var manager = Manager(offline, noDownload, delegate { throw new Exception("No debe ejecutar."); }, pendingCache, delegate { return now; }))
        {
            Check(manager.Available != null, "Conserva el aviso pendiente al reabrir la app");
            await manager.InstallAsync();
            Check(manager.Phase == UpdatePhase.Error && !manager.IsBusy && noDownload.Calls == 0 && manager.Available != null, "Sin red informa del error y mantiene el aviso sin ejecutar una caché antigua");
        }
        var downloadFailure = new StubDownload { Fail = true };
        using (var manager = Manager(new StubFeed { Release = release }, downloadFailure, delegate { throw new Exception("No debe ejecutar."); }, Cache(), delegate { return now; }))
        {
            await manager.InstallAsync();
            Check(manager.Phase == UpdatePhase.Error && !manager.IsBusy, "Una descarga fallida deja disponible el reintento");
        }
        var waiting = new StubFeed { Pending = new TaskCompletionSource<UpdateRelease>() };
        using (var manager = Manager(waiting, new StubDownload(), delegate { }, Cache(), delegate { return now; }))
        {
            var first = manager.CheckAsync();
            await manager.CheckAsync();
            Check(waiting.Calls == 1 && manager.IsBusy, "Agrupa búsquedas simultáneas sin peticiones duplicadas");
            waiting.Pending.SetResult(null);
            await first;
        }
        var canceledFeed = new StubFeed { Pending = new TaskCompletionSource<UpdateRelease>() };
        var canceledManager = Manager(canceledFeed, new StubDownload(), delegate { throw new Exception("No debe ejecutar."); }, Cache(), delegate { return now; });
        var canceledTask = canceledManager.CheckAsync(); canceledManager.Dispose(); await canceledTask;
        Check(true, "Cerrar la app cancela las búsquedas pendientes sin ejecutar ni bloquear");
    }
}
