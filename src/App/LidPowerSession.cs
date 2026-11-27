using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

internal interface ILidPowerPlan
{
    Guid ActiveScheme();
    void CheckAccess(bool onBattery);
    uint Read(Guid scheme, bool onBattery);
    void Write(Guid scheme, bool onBattery, uint value);
    void Refresh(Guid scheme);
}

internal sealed class WindowsLidPowerPlan : ILidPowerPlan
{
    private static readonly Guid Buttons = new Guid("4f971e89-eebd-4455-a8de-9e59040e7347");
    private static readonly Guid Lid = new Guid("5ca83367-6e45-459f-a27b-476b1d01c936");

    public Guid ActiveScheme()
    {
        IntPtr pointer;
        Check(PowerGetActiveScheme(IntPtr.Zero, out pointer));
        try { return (Guid)Marshal.PtrToStructure(pointer, typeof(Guid)); }
        finally { LocalFree(pointer); }
    }

    public void CheckAccess(bool onBattery)
    {
        var setting = Lid;
        Check(PowerSettingAccessCheck(onBattery ? 1u : 0u, ref setting));
    }

    public uint Read(Guid scheme, bool onBattery)
    {
        var subgroup = Buttons;
        var setting = Lid;
        uint value;
        Check(onBattery
            ? PowerReadDCValueIndex(IntPtr.Zero, ref scheme, ref subgroup, ref setting, out value)
            : PowerReadACValueIndex(IntPtr.Zero, ref scheme, ref subgroup, ref setting, out value));
        return value;
    }

    public void Write(Guid scheme, bool onBattery, uint value)
    {
        var subgroup = Buttons;
        var setting = Lid;
        Check(onBattery
            ? PowerWriteDCValueIndex(IntPtr.Zero, ref scheme, ref subgroup, ref setting, value)
            : PowerWriteACValueIndex(IntPtr.Zero, ref scheme, ref subgroup, ref setting, value));
    }

    public void Refresh(Guid scheme)
    {
        if (ActiveScheme() == scheme) Check(PowerSetActiveScheme(IntPtr.Zero, ref scheme));
    }

    private static void Check(uint status)
    {
        if (status == 1260) throw new Win32Exception((int)status, "Tu organización gestiona la acción de cierre de la tapa.");
        if (status != 0) throw new Win32Exception((int)status);
    }

    [DllImport("powrprof.dll")] private static extern uint PowerGetActiveScheme(IntPtr root, out IntPtr scheme);
    [DllImport("powrprof.dll")] private static extern uint PowerSetActiveScheme(IntPtr root, ref Guid scheme);
    [DllImport("powrprof.dll")] private static extern uint PowerSettingAccessCheck(uint access, ref Guid setting);
    [DllImport("powrprof.dll")] private static extern uint PowerReadACValueIndex(IntPtr root, ref Guid scheme, ref Guid subgroup, ref Guid setting, out uint value);
    [DllImport("powrprof.dll")] private static extern uint PowerReadDCValueIndex(IntPtr root, ref Guid scheme, ref Guid subgroup, ref Guid setting, out uint value);
    [DllImport("powrprof.dll")] private static extern uint PowerWriteACValueIndex(IntPtr root, ref Guid scheme, ref Guid subgroup, ref Guid setting, uint value);
    [DllImport("powrprof.dll")] private static extern uint PowerWriteDCValueIndex(IntPtr root, ref Guid scheme, ref Guid subgroup, ref Guid setting, uint value);
    [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr memory);
}

internal sealed class LidPowerOverride
{
    private const uint DoNothing = 0;
    private readonly ILidPowerPlan power;
    private readonly string backup;

    internal LidPowerOverride(ILidPowerPlan power, string backup)
    {
        this.power = power;
        this.backup = backup;
    }

    internal void Apply()
    {
        var scheme = power.ActiveScheme();
        uint pluggedIn = power.Read(scheme, false), onBattery = power.Read(scheme, true);
        if (pluggedIn == DoNothing && onBattery == DoNothing) return;
        if (pluggedIn != DoNothing) power.CheckAccess(false);
        if (onBattery != DoNothing) power.CheckAccess(true);

        string temp = backup + ".tmp";
        try
        {
            using (var file = File.Create(temp))
            using (var writer = new BinaryWriter(file))
            {
                writer.Write(scheme.ToByteArray());
                writer.Write(pluggedIn);
                writer.Write(onBattery);
                writer.Flush();
                file.Flush(true);
            }
            File.Move(temp, backup);
        }
        finally { File.Delete(temp); }

        try
        {
            if (pluggedIn != DoNothing) power.Write(scheme, false, DoNothing);
            if (onBattery != DoNothing) power.Write(scheme, true, DoNothing);
            power.Refresh(scheme);
        }
        catch (Exception applyError)
        {
            try { Restore(); }
            catch (Exception restoreError)
            {
                throw new IOException("No se pudo aplicar ni restaurar el ajuste de la tapa.",
                    new AggregateException(applyError, restoreError));
            }
            throw;
        }
    }

    internal void Restore()
    {
        if (!File.Exists(backup)) return;
        Guid scheme;
        uint pluggedIn, onBattery;
        using (var file = new BinaryReader(File.OpenRead(backup)))
        {
            scheme = new Guid(file.ReadBytes(16));
            pluggedIn = file.ReadUInt32();
            onBattery = file.ReadUInt32();
        }
        try
        {
            RestoreValue(scheme, false, pluggedIn);
        }
        finally
        {
            try { RestoreValue(scheme, true, onBattery); }
            finally { power.Refresh(scheme); }
        }
        File.Delete(backup);
    }

    private void RestoreValue(Guid scheme, bool onBattery, uint original)
    {
        if (original == DoNothing) return;
        uint current;
        try { current = power.Read(scheme, onBattery); }
        catch (Win32Exception ex)
        {
            // Un plan eliminado ya no conserva estos valores.
            if (ex.NativeErrorCode == 2 || ex.NativeErrorCode == 1168) return;
            throw;
        }
        if (current != DoNothing) return;
        power.CheckAccess(onBattery);
        power.Write(scheme, onBattery, original);
    }
}

internal sealed class LidPowerSession
{
    private const string GuardArgument = "--lid-guard";
    private const string RecoverArgument = "--lid-recover";
    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppIdentity.DirectoryName);
    private static readonly string BackupPath = Path.Combine(DirectoryPath, "energia-tapa.dat");
    private readonly Process guard;

    private LidPowerSession(Process guard) { this.guard = guard; }

    internal static LidPowerSession Start(bool enabled)
    {
        if (!enabled && !File.Exists(BackupPath)) return null;
        var child = Process.Start(new ProcessStartInfo(Assembly.GetExecutingAssembly().Location, enabled ? GuardArgument : RecoverArgument)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            StandardOutputEncoding = Encoding.UTF8
        });
        var session = new LidPowerSession(child);
        try
        {
            Task<string> response = child.StandardOutput.ReadLineAsync();
            if (!response.Wait(5000)) throw new IOException("Windows tardó demasiado en aplicar el ajuste de la tapa.");
            string status = response.Result;
            if (status == null) throw new IOException("No se pudo iniciar el ajuste de la tapa.");
            if (status != "OK") throw new IOException(status);
        }
        catch
        {
            // Cerrar la entrada también revierte los cambios si el hijo termina de aplicarlos después del plazo.
            try { child.StandardInput.Close(); }
            catch (IOException) { }
            finally { child.Dispose(); }
            throw;
        }
        if (enabled) return session;
        session.Stop();
        return null;
    }

    internal void Stop()
    {
        using (guard)
        {
            guard.StandardInput.Close();
            if (!guard.WaitForExit(5000))
                throw new IOException("Windows tardó demasiado en restaurar el ajuste de la tapa.");
            if (guard.ExitCode != 0)
            {
                string message = guard.StandardOutput.ReadLine();
                throw new IOException(message ?? "No se pudo restaurar el ajuste de la tapa.");
            }
        }
    }

    internal static bool IsWorker(string argument) { return argument == GuardArgument || argument == RecoverArgument; }

    internal static void RunSession(LidPowerOverride power, bool enabled, Stream input, TextWriter output)
    {
        power.Restore();
        if (enabled) power.Apply();
        try
        {
            output.WriteLine("OK");
            // El sistema cierra la canalización aunque el proceso principal termine de golpe.
            if (enabled) while (input.ReadByte() != -1) { }
        }
        finally { if (enabled) power.Restore(); }
    }

    internal static int RunWorker(string argument)
    {
        using (var output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true })
        {
            try
            {
                Directory.CreateDirectory(DirectoryPath);
                // Dos instancias en sesiones distintas no deben sobrescribir la misma copia de recuperación.
                using (new FileStream(BackupPath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None,
                    1, FileOptions.DeleteOnClose))
                {
                    var power = new LidPowerOverride(new WindowsLidPowerPlan(), BackupPath);
                    using (var input = Console.OpenStandardInput())
                        RunSession(power, argument == GuardArgument, input, output);
                }
                return 0;
            }
            catch (Exception ex)
            {
                UpdateLog.Write("ajuste-tapa", ex);
                try { output.WriteLine(ex.Message.Replace('\r', ' ').Replace('\n', ' ')); }
                catch (IOException) { }
                return 1;
            }
        }
    }
}
