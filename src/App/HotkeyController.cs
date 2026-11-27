using System;
using System.IO;

// Autor: Miquel Garrigo
internal sealed partial class LockController
{
    private HotkeyRegistration hotkeys;
    private string hotkeyError;
    internal bool HotkeyRegistered { get { return hotkeys != null && hotkeys.IsRegistered; } }
    internal int VirtualKey { get { return hotkeys == null ? 0 : hotkeys.ActivationKey; } }

    private void InitializeHotkeys()
    {
        hotkeys = new HotkeyRegistration(new WindowsHotkeyPlatform(source.Handle));
        try { using (var change = hotkeys.Prepare(settings)) change.Commit(); }
        catch (ShortcutException ex) { hotkeyError = ex.Message; }
    }

    private void SavePreferences(AppSettings updated, bool desiredStartup)
    {
        // Mantener los atajos anteriores hasta que los nuevos y el guardado estén listos.
        using (var change = hotkeys.Prepare(updated))
        {
            bool previousStartup = Startup.Enabled;
            bool startupChanged = previousStartup != desiredStartup;
            bool previousLidPower = lidPower != null;
            bool lidChanged = settings.KeepRunningWithLidClosed != updated.KeepRunningWithLidClosed;
            bool lidAttempted = false;
            try
            {
                if (startupChanged) Startup.Enabled = desiredStartup;
                if (lidChanged)
                {
                    lidAttempted = true;
                    try { SetLidPower(updated.KeepRunningWithLidClosed); }
                    catch (Exception ex) { throw new IOException("Cierre de tapa. " + ex.Message, ex); }
                }
                updated.Save();
                change.Commit(); hotkeyError = null;
            }
            catch (Exception original)
            {
                Exception rollback = null;
                if (lidAttempted)
                {
                    try { SetLidPower(previousLidPower); }
                    catch (Exception ex) { ReportLidPowerError(ex); rollback = ex; }
                }
                if (startupChanged)
                {
                    try { Startup.Enabled = previousStartup; }
                    catch (Exception ex) { rollback = rollback == null ? ex : new AggregateException(rollback, ex); }
                }
                if (rollback != null)
                    throw new IOException(original.Message + " No se pudieron restaurar todos los ajustes anteriores.",
                        new AggregateException(original, rollback));
                throw;
            }
        }
    }
}
