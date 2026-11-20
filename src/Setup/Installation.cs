using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// Autor: Miquel Garrigo
internal interface IInstallHost
{
    string StartupCommand { get; set; }
    void StopApplication();
    void Launch(string executable);
}

internal sealed class PackageInstallation
{
    internal const string Defaults = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<AppSettings>\n  <DeviceName />\n  <TimeoutSeconds>20</TimeoutSeconds>\n  <KeepAwake>true</KeepAwake>\n  <ShowClock>true</ShowClock>\n  <ImagePath />\n  <SimulateActivity>true</SimulateActivity>\n  <ActivityIntervalSeconds>300</ActivityIntervalSeconds>\n</AppSettings>\n";
    internal readonly string DirectoryPath;
    internal string ExecutablePath { get { return OwnedPath("PantallaBloqueada.exe"); } }
    internal string ExpectedStartup { get { return "\"" + ExecutablePath + "\" --background"; } }
    private readonly byte[] application;
    private readonly byte[] uninstaller;

    internal PackageInstallation(string directory, byte[] app, byte[] remove)
    {
        DirectoryPath = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!string.Equals(Path.GetFileName(DirectoryPath), "PantallaBloqueada", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(DirectoryPath, Path.GetPathRoot(DirectoryPath), StringComparison.OrdinalIgnoreCase))
            throw new IOException("La carpeta de instalación no es válida.");
        application = app;
        uninstaller = remove;
    }

    private string OwnedPath(string name)
    {
        string path = Path.GetFullPath(Path.Combine(DirectoryPath, name));
        if (!string.Equals(Path.GetDirectoryName(path), DirectoryPath, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Ruta fuera de la carpeta de la aplicación.");
        if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("No se puede modificar un enlace en la carpeta de la aplicación.");
        return path;
    }

    private void ValidateDirectory()
    {
        if (Directory.Exists(DirectoryPath) && (File.GetAttributes(DirectoryPath) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("La carpeta de la aplicación no puede ser un enlace.");
    }

    internal void Install(IInstallHost host, bool updating = false, string previousSettingsPath = null)
    {
        ValidateDirectory();
        if (application == null || application.Length < 2 || application[0] != 0x4d || application[1] != 0x5a ||
            uninstaller == null || uninstaller.Length < 2 || uninstaller[0] != 0x4d || uninstaller[1] != 0x5a)
            throw new IOException("El paquete está incompleto.");
        var backups = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        string previousStartup = host.StartupCommand;
        bool existed = Directory.Exists(DirectoryPath);
        bool startupChanged = false;
        host.StopApplication();
        Directory.CreateDirectory(DirectoryPath);
        try
        {
            Store("PantallaBloqueada.exe", application, backups);
            Store("Desinstalar.exe", uninstaller, backups);
            if (!File.Exists(OwnedPath("ajustes.xml")))
                Store("ajustes.xml", previousSettingsPath != null && File.Exists(previousSettingsPath)
                    ? File.ReadAllBytes(previousSettingsPath) : new UTF8Encoding(false).GetBytes(Defaults), backups);
            // Actualizar respeta si el usuario había desactivado el inicio automático.
            bool wasEnabled = previousStartup != null && previousStartup.EndsWith("PantallaBloqueada.exe\" --background", StringComparison.OrdinalIgnoreCase);
            if (!updating || wasEnabled)
            {
                host.StartupCommand = ExpectedStartup;
                startupChanged = true;
            }
            host.Launch(ExecutablePath);
        }
        catch
        {
            foreach (var item in backups)
            {
                if (item.Value == null) { if (File.Exists(item.Key)) File.Delete(item.Key); }
                else AtomicWrite(item.Key, item.Value);
            }
            if (startupChanged) host.StartupCommand = previousStartup;
            if (!existed && Directory.Exists(DirectoryPath) && Directory.GetFileSystemEntries(DirectoryPath).Length == 0)
                Directory.Delete(DirectoryPath, false);
            throw;
        }
    }

    private void Store(string name, byte[] data, IDictionary<string, byte[]> backups)
    {
        string path = OwnedPath(name);
        byte[] previous = File.Exists(path) ? File.ReadAllBytes(path) : null;
        AtomicWrite(path, data);
        backups.Add(path, previous);
    }

    private static void AtomicWrite(string path, byte[] data)
    {
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllBytes(temp, data);
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    internal void Uninstall(IInstallHost host)
    {
        ValidateDirectory();
        if (Directory.Exists(DirectoryPath)) host.StopApplication();
        if (string.Equals(host.StartupCommand, ExpectedStartup, StringComparison.OrdinalIgnoreCase)) host.StartupCommand = null;
        foreach (string name in new[] { "PantallaBloqueada.exe", "Desinstalar.exe", "ajustes.xml", "updates.xml" })
        {
            string path = OwnedPath(name);
            if (File.Exists(path)) File.Delete(path);
        }
        if (Directory.Exists(DirectoryPath) && Directory.GetFileSystemEntries(DirectoryPath).Length == 0)
            Directory.Delete(DirectoryPath, false);
    }
}
