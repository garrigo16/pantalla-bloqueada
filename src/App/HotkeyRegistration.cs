using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// Autor: Miquel Garrigo
internal sealed class ShortcutException : Exception
{ internal ShortcutException(string message) : base(message) { } }
internal enum ShortcutAction { None, Toggle, Activate, Unlock, Game }
internal interface IHotkeyPlatform
{
    int ResolveEnye();
    bool Register(int id, int modifiers, int key, out int error);
    void Unregister(int id);
}
internal struct HotkeyChord : IEquatable<HotkeyChord>
{
    internal readonly int Modifiers, Key;
    internal HotkeyChord(int modifiers, int key) { Modifiers = modifiers; Key = key; }
    public bool Equals(HotkeyChord other) { return Modifiers == other.Modifiers && Key == other.Key; }
    public override bool Equals(object value) { return value is HotkeyChord && Equals((HotkeyChord)value); }
    public override int GetHashCode() { return (Modifiers * 397) ^ Key; }
}

internal sealed class HotkeyRegistration : IDisposable
{
    private readonly IHotkeyPlatform platform;
    private Dictionary<HotkeyChord, int> bindings = new Dictionary<HotkeyChord, int>();
    private HotkeyChord activate, unlock, game;
    private int nextId = 172;
    private bool disposed;
    private Change pending;
    internal bool IsRegistered { get { return bindings.Count > 0 && !disposed; } }
    internal int ActivationKey { get { return activate.Key; } }
    internal HotkeyRegistration(IHotkeyPlatform native) { platform = native; }

    internal Change Prepare(AppSettings preferences)
    {
        if (disposed) throw new ObjectDisposedException("HotkeyRegistration");
        if (pending != null) throw new InvalidOperationException("Ya hay un cambio de atajos pendiente.");
        var first = new HotkeySpec(preferences.ActivateModifiers, preferences.ActivateKey);
        var second = preferences.SameHotkey ? first : new HotkeySpec(preferences.UnlockModifiers, preferences.UnlockKey);
        if (first.Error != null) throw new ShortcutException(first.Error);
        if (second.Error != null) throw new ShortcutException(second.Error);
        if ((second.Modifiers & 4) != 0) throw new ShortcutException("Deja Shift libre en el atajo de salida: se usa para abrir el pinball.");
        int enye = first.Key == 0 || second.Key == 0 ? platform.ResolveEnye() : 0;
        var active = new HotkeyChord(first.Modifiers, first.Key == 0 ? enye : first.Key);
        var inactive = new HotkeyChord(second.Modifiers, second.Key == 0 ? enye : second.Key);
        var play = new HotkeyChord(inactive.Modifiers | 4, inactive.Key);
        if (active.Equals(play)) throw new ShortcutException("El atajo de activación coincide con el del pinball. Elige otra combinación.");
        var proposed = new Dictionary<HotkeyChord, int>();
        var created = new List<int>();
        try
        {
            Add(active, first.ToString(), proposed, created);
            Add(inactive, second.ToString(), proposed, created);
            Add(play, new HotkeySpec(second.Modifiers | 4, second.Key).ToString(), proposed, created);
            pending = new Change(this, proposed, created, active, inactive, play);
            return pending;
        }
        catch { foreach (int id in created) platform.Unregister(id); throw; }
    }

    private void Add(HotkeyChord chord, string label, Dictionary<HotkeyChord, int> proposed, List<int> created)
    {
        if (proposed.ContainsKey(chord)) return;
        int id;
        if (!bindings.TryGetValue(chord, out id))
        {
            do { id = nextId++; if (nextId > 0xbfff) nextId = 172; } while (bindings.ContainsValue(id) || created.Contains(id));
            int error;
            if (!platform.Register(id, chord.Modifiers, chord.Key, out error))
                throw new ShortcutException("No se pudo reservar " + label + ". Puede estar en uso por Windows u otra aplicación. Elige otra combinación (error " + error + ").");
            created.Add(id);
        }
        proposed.Add(chord, id);
    }

    internal ShortcutAction ActionFor(int id)
    {
        int first, second, third;
        if (disposed || !bindings.TryGetValue(activate, out first) || !bindings.TryGetValue(unlock, out second) || !bindings.TryGetValue(game, out third)) return ShortcutAction.None;
        if (id == third) return ShortcutAction.Game;
        if (id == first && id == second) return ShortcutAction.Toggle;
        if (id == first) return ShortcutAction.Activate;
        return id == second ? ShortcutAction.Unlock : ShortcutAction.None;
    }

    internal sealed class Change : IDisposable
    {
        private readonly HotkeyRegistration owner;
        private readonly Dictionary<HotkeyChord, int> proposed;
        private readonly List<int> created;
        private readonly HotkeyChord activate, unlock, game;
        private bool done;
        internal Change(HotkeyRegistration manager, Dictionary<HotkeyChord, int> target, List<int> additions, HotkeyChord first, HotkeyChord second, HotkeyChord third)
        { owner = manager; proposed = target; created = additions; activate = first; unlock = second; game = third; }
        internal void Commit()
        {
            if (done || owner.disposed) throw new ObjectDisposedException("HotkeyRegistration.Change");
            foreach (int id in owner.bindings.Values) if (!proposed.ContainsValue(id)) owner.platform.Unregister(id);
            owner.bindings = proposed; owner.activate = activate; owner.unlock = unlock; owner.game = game;
            done = true; owner.pending = null;
        }
        public void Dispose()
        {
            if (done) return;
            foreach (int id in created) owner.platform.Unregister(id);
            done = true; owner.pending = null;
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        if (pending != null) pending.Dispose();
        foreach (int id in bindings.Values) platform.Unregister(id);
        bindings.Clear(); disposed = true;
    }
}

internal sealed class WindowsHotkeyPlatform : IHotkeyPlatform
{
    private readonly IntPtr window;
    internal WindowsHotkeyPlatform(IntPtr handle) { window = handle; }
    public int ResolveEnye()
    {
        uint thread = Native.GetWindowThreadProcessId(Native.GetForegroundWindow(), IntPtr.Zero);
        short mapping = Native.VkKeyScanEx('ñ', Native.GetKeyboardLayout(thread));
        if (mapping == -1 || (mapping >> 8) != 0) throw new ShortcutException("Este teclado no dispone de Ñ. Elige otra tecla en Atajos de teclado.");
        return mapping & 0xff;
    }
    public bool Register(int id, int modifiers, int key, out int error)
    {
        bool result = Native.RegisterHotKey(window, id, (uint)(modifiers | 0x4000), (uint)key);
        error = result ? 0 : Marshal.GetLastWin32Error(); return result;
    }
    public void Unregister(int id) { Native.UnregisterHotKey(window, id); }
}
