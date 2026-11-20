using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

// Autor: Miquel Garrigo
internal static class SetupProgram
{
    [STAThread]
    private static int Main(string[] args)
    {
        bool owns;
        using (var gate = new Mutex(true, "Local\\PantallaBloqueada.Setup.v1", out owns))
        {
            if (!owns)
            {
                if (Array.IndexOf(args, "--uninstall-worker") < 0) return 0;
                try { owns = gate.WaitOne(15000); }
                catch (AbandonedMutexException) { owns = true; }
                if (!owns) return 1;
            }
            try { return Run(args); }
            finally { gate.ReleaseMutex(); }
        }
    }

    private static int Run(string[] args)
    {
        bool silent = Array.IndexOf(args, "--silent") >= 0;
        bool worker = Array.IndexOf(args, "--uninstall-worker") >= 0;
        bool uninstall = worker || Array.IndexOf(args, "--uninstall") >= 0;
        bool updating = Array.IndexOf(args, "--update") >= 0;
        string previousSettings = null;
        int previousSettingsArgument = Array.IndexOf(args, "--settings-from");
        if (previousSettingsArgument >= 0 && previousSettingsArgument + 1 < args.Length) previousSettings = args[previousSettingsArgument + 1];
#if UNINSTALLER
        uninstall = true;
#endif
        try
        {
            Application.EnableVisualStyles();
            string appDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PantallaBloqueada");
            if (worker)
            {
                int parentId;
                if (args.Length < 2 || !int.TryParse(args[1], out parentId)) throw new IOException("Falta el proceso de desinstalación.");
                try
                {
                    using (var parent = Process.GetProcessById(parentId))
                        if (!parent.WaitForExit(10000)) throw new IOException("Cierra la ventana de desinstalación y vuelve a intentarlo.");
                }
                catch (ArgumentException) { }
            }
            string self = Assembly.GetExecutingAssembly().Location;
            if (uninstall && string.Equals(Path.GetDirectoryName(self), appDirectory, StringComparison.OrdinalIgnoreCase))
            {
                // El ayudante temporal permite retirar también el desinstalador instalado.
                string helper = Path.Combine(Path.GetTempPath(), "PantallaBloqueada-Desinstalar-" + Guid.NewGuid().ToString("N") + ".exe");
                File.Copy(self, helper, false);
                Process.Start(new ProcessStartInfo(helper, "--uninstall-worker " + Process.GetCurrentProcess().Id + (silent ? " --silent" : ""))
                    { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden });
                return 0;
            }
            byte[] app = uninstall ? null : ReadResource("Application.exe");
            byte[] remove = uninstall ? null : ReadResource("Uninstaller.exe");
            var package = new PackageInstallation(appDirectory, app, remove);
            var host = new WindowsInstallHost();
            if (uninstall) package.Uninstall(host);
            else package.Install(host, updating, previousSettings);
            if (!silent) ShowDone(uninstall, updating);
            return 0;
        }
        catch (Exception ex)
        {
            if (!silent) MessageBox.Show("No se pudo completar la operación.\n\n" + ex.Message,
                "Pantalla bloqueada", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return 1;
        }
    }

    internal static byte[] ReadResource(string name)
    {
        using (var input = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
        {
            if (input == null) throw new IOException("Falta un componente del paquete.");
            using (var output = new MemoryStream()) { input.CopyTo(output); return output.ToArray(); }
        }
    }

    private static void ShowDone(bool removed, bool updated)
    {
        using (var window = new Form())
        using (var timer = new System.Windows.Forms.Timer { Interval = 4500 })
        {
            window.Text = "Pantalla bloqueada";
            window.ClientSize = new Size(440, 190);
            window.StartPosition = FormStartPosition.CenterScreen;
            window.FormBorderStyle = FormBorderStyle.FixedDialog;
            window.MaximizeBox = false;
            window.MinimizeBox = false;
            window.BackColor = Color.FromArgb(16, 23, 40);
            window.Controls.Add(new Label { Text = removed ? "Aplicación desinstalada" : (updated ? "Actualización completada" : "Todo listo"),
                ForeColor = Color.White, Font = new Font("Segoe UI", 20, FontStyle.Bold),
                AutoSize = true, Location = new Point(24, 20) });
            window.Controls.Add(new Label
            {
                Text = removed ? "Se ha retirado la aplicación y su inicio automático." :
                    (updated ? "Versión " + ProductVersion.Current + " instalada. Tus ajustes se han conservado.\nConfigúrala desde el icono del área de notificación." :
                    "La aplicación ya está activa e iniciará con Windows.\nConfigúrala desde el icono del área de notificación."),
                ForeColor = Color.FromArgb(190, 205, 230), Font = new Font("Segoe UI", 10),
                Size = new Size(395, 60), Location = new Point(26, 76)
            });
            window.Controls.Add(new Label { Text = removed ? "Lo entendemos. Pasa en las mejores oficinas." : "Lo que pasa en el escritorio, se queda en el escritorio.",
                ForeColor = Color.FromArgb(150, 175, 210), Font = new Font("Segoe UI", 9),
                Size = new Size(395, 30), Location = new Point(26, 147) });
            timer.Tick += delegate { window.Close(); };
            window.Shown += delegate { timer.Start(); };
            Application.Run(window);
        }
    }
}

internal sealed class WindowsInstallHost : IInstallHost
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public string StartupCommand
    {
        get { using (var key = Registry.CurrentUser.OpenSubKey(RunKey)) return key == null ? null : key.GetValue("PantallaBloqueada") as string; }
        set
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (value == null) key.DeleteValue("PantallaBloqueada", false);
                else key.SetValue("PantallaBloqueada", value, RegistryValueKind.String);
            }
        }
    }

    public void StopApplication()
    {
        var watch = Stopwatch.StartNew();
        while (IsRunning())
        {
            try { using (var quit = EventWaitHandle.OpenExisting("Local\\PantallaBloqueada.Cerrar.v2")) quit.Set(); }
            catch (WaitHandleCannotBeOpenedException) { }
            if (watch.Elapsed.TotalSeconds >= 12) throw new IOException("Cierra la aplicación desde su icono y vuelve a iniciar la instalación.");
            Thread.Sleep(100);
        }
    }

    private static bool IsRunning()
    {
        try
        {
            using (var mutex = Mutex.OpenExisting("Local\\PantallaBloqueada.Residente.v2"))
            {
                try
                {
                    if (!mutex.WaitOne(0)) return true;
                    mutex.ReleaseMutex();
                    return false;
                }
                catch (AbandonedMutexException) { mutex.ReleaseMutex(); return false; }
            }
        }
        catch (WaitHandleCannotBeOpenedException) { return false; }
    }

    public void Launch(string executable)
    {
        using (var process = Process.Start(new ProcessStartInfo(executable, "--background")
            { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(executable), CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden }))
        {
            var watch = Stopwatch.StartNew();
            while (watch.Elapsed.TotalSeconds < 12)
            {
                if (process.HasExited) throw new IOException("La aplicación no pudo iniciarse.");
                try
                {
                    using (var ready = EventWaitHandle.OpenExisting("Local\\PantallaBloqueada.Configurar.v2")) return;
                }
                catch (WaitHandleCannotBeOpenedException) { Thread.Sleep(100); }
            }
            if (!process.HasExited) { process.Kill(); process.WaitForExit(3000); }
            throw new IOException("No se pudo activar el atajo. Comprueba el teclado y si otra aplicación lo utiliza.");
        }
    }
}
