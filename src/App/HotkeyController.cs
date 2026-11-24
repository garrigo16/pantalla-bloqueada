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
            try
            {
                if (startupChanged) Startup.Enabled = desiredStartup;
                updated.Save();
                change.Commit(); hotkeyError = null;
            }
            catch (Exception original)
            {
                if (startupChanged)
                {
                    try { Startup.Enabled = previousStartup; }
                    catch (Exception restoration) { throw new IOException("No se pudo guardar ni restaurar el inicio automático.", new AggregateException(original, restoration)); }
                }
                throw;
            }
        }
    }
}
