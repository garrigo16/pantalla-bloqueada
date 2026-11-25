using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

// Autor: Miquel Garrigo
internal static class Bootstrap
{
    [STAThread] private static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        bool owns;
        using (var gate = new Mutex(true, "Local\\DrifftAnalyzer.Bootstrap.v1", out owns))
        {
            if (!owns) return 2;
            try
            {
                var window = new Form { Text = AppIdentity.DisplayName, Width = 470, Height = 210, StartPosition = FormStartPosition.CenterScreen,
                    FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
                var label = new Label { Left = 24, Top = 24, Width = 410, Height = 54, Text = "Preparando Drifft Analyzer " + ProductVersion.Current + "…", Font = new Font("Segoe UI", 11) };
                var progress = new ProgressBar { Left = 24, Top = 90, Width = 410, Height = 18, Style = ProgressBarStyle.Marquee };
                var close = new Button { Left = 324, Top = 123, Width = 110, Text = "Cancelar" };
                var cancellation = new CancellationTokenSource();
                bool installing = false, finished = false; int exitCode = 1;
                window.Controls.AddRange(new Control[] { label, progress, close });
                close.Click += delegate { if (!installing) cancellation.Cancel(); };
                window.FormClosing += delegate(object sender, FormClosingEventArgs e) { if (!finished) { e.Cancel = true; if (!installing) cancellation.Cancel(); } };
                window.Shown += async delegate
                {
                    try
                    {
                        string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ReleaseSource.FullInstallerName);
                        var release = new UpdateRelease { Tag = "v" + ProductVersion.Current, Version = ProductVersion.Current,
                            AssetName = ReleaseSource.FullInstallerName, Sha256 = BootstrapPackage.Sha256, Size = BootstrapPackage.Size,
                            DownloadUrl = ReleaseSource.RepositoryUrl + "/releases/download/v" + ProductVersion.Current + "/" + ReleaseSource.FullInstallerName };
                        if (!Matches(file))
                        {
                            label.Text = "Descargando y comprobando la aplicación completa…\nTus ajustes y la app actual se conservan.";
                            using (var download = new InstallerDownload()) file = await download.DownloadAsync(release, cancellation.Token);
                        }
                        cancellation.Token.ThrowIfCancellationRequested();
                        installing = true; close.Enabled = false;
                        label.Text = "Instalando la versión " + ProductVersion.Current + "…";
                        string arguments = "";
                        foreach (string arg in args)
                        {
                            // Las rutas Windows no pueden contener comillas ni terminan en barra para este protocolo.
                            if (arg.IndexOf('"') >= 0 || arg.EndsWith("\\", StringComparison.Ordinal)) throw new IOException("Argumento de instalación no válido.");
                            arguments += " \"" + arg + "\"";
                        }
                        if (Array.IndexOf(args, "--silent") < 0) arguments += " --silent";
                        using (var process = Process.Start(new ProcessStartInfo(file, arguments.Trim()) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(file) }))
                        {
                            if (process == null) throw new IOException("No se pudo iniciar la instalación.");
                            await Task.Run(delegate { if (!process.WaitForExit(120000)) throw new IOException("La instalación está tardando demasiado."); });
                            exitCode = process.ExitCode;
                            if (exitCode != 0) throw new IOException("No se pudo completar la instalación. La versión anterior se conserva.");
                        }
                        if (Array.IndexOf(args, "--silent") < 0)
                            MessageBox.Show("Drifft Analyzer " + ProductVersion.Current + " está instalado. Puedes configurarlo desde el icono del área de notificación.", AppIdentity.DisplayName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (OperationCanceledException) { exitCode = 1; }
                    catch (Exception ex)
                    {
                        UpdateLog.Write("instalador-inicial", ex); exitCode = 1;
                        MessageBox.Show("No se pudo completar la actualización. La app anterior y tus ajustes se conservan.\n\nPuedes abrir Iniciar.exe otra vez, o descargar DrifftAnalyzer-Setup.exe desde la misma página de versiones para instalar sin esta descarga inicial.", AppIdentity.DisplayName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    finally { installing = false; finished = true; window.Close(); }
                };
                Application.Run(window); cancellation.Dispose();
                if (exitCode != 0 && Array.IndexOf(args, "--update") >= 0) RecoverPrevious();
                return exitCode;
            }
            finally { gate.ReleaseMutex(); }
        }
    }
    private static void RecoverPrevious()
    {
        // Las copias antiguas no supervisaban la salida del instalador: reiniciarlas quita el estado ocupado.
        try
        {
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppIdentity.DirectoryName);
            string previous = Path.Combine(directory, AppIdentity.ExecutableName);
            if (!File.Exists(previous)) previous = Path.Combine(directory, AppIdentity.LegacyExecutableName);
            if (!File.Exists(previous)) return;
            try { using (var signal = EventWaitHandle.OpenExisting("Local\\PantallaBloqueada.Cerrar.v2")) signal.Set(); }
            catch (WaitHandleCannotBeOpenedException) { }
            var watch = Stopwatch.StartNew();
            while (watch.Elapsed.TotalSeconds < 12)
            {
                bool running = false;
                try
                {
                    using (var mutex = Mutex.OpenExisting("Local\\PantallaBloqueada.Residente.v2"))
                    {
                        try { running = !mutex.WaitOne(0); if (!running) mutex.ReleaseMutex(); }
                        catch (AbandonedMutexException) { mutex.ReleaseMutex(); }
                    }
                }
                catch (WaitHandleCannotBeOpenedException) { }
                if (!running) { Process.Start(new ProcessStartInfo(previous, "--config") { UseShellExecute = false, WorkingDirectory = directory }); return; }
                Thread.Sleep(100);
            }
        }
        catch (Exception ex) { UpdateLog.Write("recuperar-actualizador-antiguo", ex); }
    }
    private static bool Matches(string file)
    {
        if (!File.Exists(file) || new FileInfo(file).Length != BootstrapPackage.Size) return false;
        using (var input = File.OpenRead(file))
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").Equals(BootstrapPackage.Sha256, StringComparison.OrdinalIgnoreCase);
    }
}
