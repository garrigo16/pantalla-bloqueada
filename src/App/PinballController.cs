using System;
using System.Windows.Input;

// Autor: Miquel Garrigo
internal sealed partial class LockController
{
    private void OpenPinball()
    {
        if (!HotkeyRegistered) { OpenSettings(); return; }
        if (windows.Count == 0) Show();
        var target = FunWindow(); if (target == null) return;
        lastPointer = null;
        try { target.StartPinball(); }
        catch { tray.ShowBalloonTip(4000, AppIdentity.DisplayName, "No se pudo abrir el pinball.", System.Windows.Forms.ToolTipIcon.Warning); }
    }
    private void FunKeyUp(object sender, KeyEventArgs e)
    {
        var target = FunWindow();
        if (target != null && target.HasPinball && (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl))
        { target.PinballInput(e.Key, false, false); e.Handled = true; }
    }
}

internal sealed partial class LockWindow
{
    private PinballGame pinball;
    internal bool HasPinball { get { return pinball != null; } }
    internal PinballGame ActivePinball { get { return pinball; } }
    internal void StartPinball()
    {
        if (!HasImage || pinball != null) return;
        if (funOverlay != null) funOverlay.Clear();
        pinball = new PinballGame(Surface, delegate { pinball = null; });
    }
    internal void PinballInput(Key key, bool down, bool repeat) { if (pinball != null) pinball.Key(key, down, repeat); }
}
