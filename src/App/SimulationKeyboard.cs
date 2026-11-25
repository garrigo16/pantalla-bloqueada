using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Interop;

// Autor: Miquel Garrigo
internal sealed class SimulationKeyRules
{
    private readonly Func<HotkeyChord, ShortcutAction> resolve;
    private readonly HashSet<int> intercepted = new HashSet<int>();
    private int windowsKeys;

    internal SimulationKeyRules(Func<HotkeyChord, ShortcutAction> resolveShortcut) { resolve = resolveShortcut; }

    internal void Reset() { windowsKeys = 0; intercepted.Clear(); }

    internal bool Intercept(int key, bool down, int modifiers, out ShortcutAction action, out bool realLock)
    {
        action = ShortcutAction.None;
        realLock = false;
        if (key == 0x5b || key == 0x5c)
        {
            int flag = key == 0x5b ? 1 : 2;
            if (down) { windowsKeys |= flag; return true; }
            bool wasIntercepted = (windowsKeys & flag) != 0;
            windowsKeys &= ~flag;
            return wasIntercepted;
        }

        // Let the operating system keep processing its secure attention sequence.
        if (key == 0x2e) return false;
        if (!down) return intercepted.Remove(key);
        if (intercepted.Contains(key)) return true;
        if (key == 0x09 && (modifiers & 1) != 0) { intercepted.Add(key); return true; }
        if (windowsKeys == 0 || IsModifier(key)) return false;

        intercepted.Add(key);
        realLock = key == 'L' && modifiers == 0;
        if (!realLock) action = resolve(new HotkeyChord(modifiers | 8, key));
        return true;
    }

    private static bool IsModifier(int key) { return (key >= 0x10 && key <= 0x12) || (key >= 0xa0 && key <= 0xa5); }
}

internal sealed class SimulationKeyboard : IDisposable
{
    private const int KeyboardLl = 13;
    private const int LlAltDown = 0x20;
    private readonly HashSet<IntPtr> windows;
    private readonly Action<ShortcutAction, bool> onShortcut;
    private readonly SimulationKeyRules rules;
    private readonly HookCallback callback;
    private IntPtr handle;

    internal SimulationKeyboard(IEnumerable<IntPtr> targets, HotkeyRegistration hotkeys, Action<ShortcutAction, bool> shortcut)
    {
        windows = new HashSet<IntPtr>(targets);
        onShortcut = shortcut;
        rules = new SimulationKeyRules(hotkeys.ActionFor);
        callback = KeyboardMessage;
        handle = SetWindowsHookEx(KeyboardLl, callback, GetModuleHandle(null), 0);
    }

    private IntPtr KeyboardMessage(int code, IntPtr message, IntPtr data)
    {
        if (code != 0) return CallNextHookEx(handle, code, message, data);
        if (!OwnsForeground())
        {
            rules.Reset();
            return CallNextHookEx(handle, code, message, data);
        }

        int kind = message.ToInt32();
        if (kind != 0x100 && kind != 0x101 && kind != 0x104 && kind != 0x105)
            return CallNextHookEx(handle, code, message, data);
        bool down = kind == 0x100 || kind == 0x104;
        int modifiers = (Marshal.ReadInt32(data, 8) & LlAltDown) != 0 ? 1 : 0;
        // The state of the previous modifier presses is available before posting this event.
        if ((Native.GetAsyncKeyState(0x11) & 0x8000) != 0) modifiers |= 2;
        if ((Native.GetAsyncKeyState(0x10) & 0x8000) != 0) modifiers |= 4;
        ShortcutAction action;
        bool realLock;
        if (!rules.Intercept(Marshal.ReadInt32(data), down, modifiers, out action, out realLock))
            return CallNextHookEx(handle, code, message, data);
        if (action != ShortcutAction.None || realLock) onShortcut(action, realLock);
        return new IntPtr(1);
    }

    internal bool OwnsForeground()
    {
        var foreground = Native.GetForegroundWindow();
        return foreground != IntPtr.Zero && (windows.Contains(foreground) || windows.Contains(GetAncestor(foreground, 2)));
    }

    public void Dispose()
    {
        if (handle == IntPtr.Zero) return;
        UnhookWindowsHookEx(handle);
        handle = IntPtr.Zero;
        rules.Reset();
    }

    private delegate IntPtr HookCallback(int code, IntPtr message, IntPtr data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int id, HookCallback procedure, IntPtr module, uint thread);
    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr handle);
    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr window, uint flags);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string name);
}

internal sealed partial class LockController
{
    private SimulationKeyboard simulationKeyboard;

    private void StartSimulationKeyboard()
    {
        var handles = new List<IntPtr>();
        foreach (var window in windows) handles.Add(new WindowInteropHelper(window).Handle);
        simulationKeyboard = new SimulationKeyboard(handles, hotkeys, delegate(ShortcutAction action, bool realLock)
        {
            var current = simulationKeyboard;
            Dispatch(delegate
            {
                if (current == null || current != simulationKeyboard || !current.OwnsForeground()) return;
                if (realLock) LockWorkStation();
                else HandleShortcut(action);
            });
        });
    }

    private void StopSimulationKeyboard()
    {
        if (simulationKeyboard == null) return;
        simulationKeyboard.Dispose();
        simulationKeyboard = null;
    }

    [DllImport("user32.dll")]
    private static extern bool LockWorkStation();
}
