using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Xml.Serialization;

// Autor: Miquel Garrigo
public sealed class UpdateState
{
    public DateTime LastAttemptUtc { get; set; }
    public DateTime LastSuccessfulUtc { get; set; }
    public UpdateRelease Available { get; set; }
}

internal enum UpdatePhase { Idle, Checking, Current, Available, Downloading, Installing, Error }

internal sealed class UpdateManager : IDisposable
{
    private readonly IReleaseFeed feed;
    private readonly IInstallerDownload download;
    private readonly Action<string, string> launch;
    private readonly Func<DateTime> utcNow;
    private readonly string statePath;
    private readonly string settingsPath;
    private readonly string currentVersion;
    private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
    private DispatcherTimer timer;
    private UpdateState state;
    private bool disposed;
    internal event EventHandler Changed;
    internal bool IsBusy { get; private set; }
    internal UpdatePhase Phase { get; private set; }
    internal string Message { get; private set; }
    internal UpdateRelease Available { get { return state.Available; } }
    internal DateTime LastCheckUtc { get { return state.LastSuccessfulUtc; } }
    internal bool IsDue
    {
        get { return state.LastAttemptUtc == DateTime.MinValue || state.LastAttemptUtc > utcNow().AddMinutes(5) || utcNow() - state.LastAttemptUtc >= TimeSpan.FromHours(12); }
    }

    internal UpdateManager() : this(new GitHubReleaseFeed(), new InstallerDownload(), StartInstaller,
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updates.xml"), AppSettings.FilePath, ProductVersion.Current, delegate { return DateTime.UtcNow; }) { }

    internal UpdateManager(IReleaseFeed releaseFeed, IInstallerDownload installerDownload, Action<string, string> start,
        string cachePath, string preferencesPath, string version, Func<DateTime> clock)
    {
        feed = releaseFeed; download = installerDownload; launch = start; statePath = cachePath;
        settingsPath = preferencesPath; currentVersion = version; utcNow = clock;
        try
        {
            using (var input = File.OpenRead(statePath)) state = (UpdateState)new XmlSerializer(typeof(UpdateState)).Deserialize(input);
            if (state == null) state = new UpdateState();
            if (state.Available != null)
            {
                state.Available.Validate();
                if (UpdateRelease.ParseVersion(state.Available.Version) <= UpdateRelease.ParseVersion(currentVersion)) state.Available = null;
            }
        }
        catch { state = new UpdateState(); }
        Phase = state.Available == null ? UpdatePhase.Idle : UpdatePhase.Available;
        Message = state.Available == null ? "Se buscan actualizaciones automáticamente cada 12 horas." : "Actualización disponible: " + state.Available.Version;
    }

    internal void Start()
    {
        if (disposed || timer != null) return;
        timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
        timer.Tick += async delegate
        {
            timer.Interval = TimeSpan.FromMinutes(30);
            if (IsDue) await CheckAsync();
        };
        timer.Start();
    }

    internal async Task CheckAsync()
    {
        if (disposed || IsBusy) return;
        IsBusy = true;
        Set(UpdatePhase.Checking, "Buscando actualizaciones…");
        try { await RefreshAsync(); }
        catch (OperationCanceledException) { if (!disposed) Set(UpdatePhase.Error, "La búsqueda tardó demasiado. Vuelve a intentarlo."); }
        catch (Exception ex) { if (!disposed) ShowError(ex); }
        finally { if (!disposed) { IsBusy = false; Notify(); } }
    }

    private async Task RefreshAsync()
    {
        state.LastAttemptUtc = utcNow();
        Save();
        var release = await feed.LatestAsync(currentVersion, cancellation.Token);
        if (disposed) return;
        if (release != null)
        {
            release.Validate();
            if (UpdateRelease.ParseVersion(release.Version) <= UpdateRelease.ParseVersion(currentVersion)) release = null;
        }
        state.LastSuccessfulUtc = utcNow();
        state.Available = release;
        Save();
        Set(release == null ? UpdatePhase.Current : UpdatePhase.Available,
            release == null ? "Ya estás en la última versión (" + currentVersion + ")." : "Actualización disponible: " + release.Version);
    }

    internal async Task InstallAsync()
    {
        if (disposed || IsBusy) return;
        IsBusy = true;
        Set(UpdatePhase.Checking, "Preparando la actualización…");
        try
        {
            await RefreshAsync();
            if (disposed || state.Available == null) return;
            var release = state.Available;
            Set(UpdatePhase.Downloading, "Descargando y comprobando la versión " + release.Version + "…");
            string file = await download.DownloadAsync(release, cancellation.Token);
            if (disposed) return;
            launch(file, settingsPath);
            Set(UpdatePhase.Installing, "Instalando " + release.Version + ". La aplicación se reiniciará conservando tus ajustes.");
        }
        catch (OperationCanceledException) { if (!disposed) Set(UpdatePhase.Error, "La descarga tardó demasiado. Puedes volver a intentarlo."); }
        catch (Exception ex) { if (!disposed) ShowError(ex); }
        finally { if (!disposed && Phase != UpdatePhase.Installing) { IsBusy = false; Notify(); } }
    }

    private static void StartInstaller(string file, string preferences)
    {
        if (Process.Start(new ProcessStartInfo(file, "--update --settings-from \"" + preferences + "\"")
            { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, WorkingDirectory = Path.GetDirectoryName(file) }) == null)
            throw new UpdateException("No se pudo abrir el instalador. La aplicación sigue disponible.");
    }

    private void ShowError(Exception ex)
    {
        var known = ex as UpdateException;
        Set(UpdatePhase.Error, known == null ? "No se pudo comprobar o descargar la actualización. Revisa la conexión y vuelve a intentarlo." : known.Message);
    }
    private void Set(UpdatePhase phase, string message) { if (disposed) return; Phase = phase; Message = message; Notify(); }
    private void Notify() { if (Changed != null) Changed(this, EventArgs.Empty); }
    private void Save()
    {
        string temp = statePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var output = File.Create(temp)) new XmlSerializer(typeof(UpdateState)).Serialize(output, state);
            if (File.Exists(statePath)) File.Replace(temp, statePath, null); else File.Move(temp, statePath);
        }
        catch { /* La búsqueda sigue funcionando si no se puede guardar la caché. */ }
        finally { try { if (File.Exists(temp)) File.Delete(temp); } catch { } }
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (timer != null) timer.Stop();
        cancellation.Cancel();
        feed.Dispose(); download.Dispose(); cancellation.Dispose();
    }
}
