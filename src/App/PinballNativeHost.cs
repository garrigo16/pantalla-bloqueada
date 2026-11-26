using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

// Autor: Miquel Garrigo
internal sealed class PinballNativeHost : HwndHost
{
    private readonly Func<IntPtr, ProcessStartInfo> startInfo;
    private readonly string statusPath;
    private readonly PokemonPinballEngine? pokemonEngine;
    private readonly Dictionary<Key, Key> pressed = new Dictionary<Key, Key>();
    private IntPtr handle, child, job;
    private int childWidth, childHeight;
    private Process process;
    private bool stopping;
    internal bool Started { get { return process != null; } }
    internal bool Exited { get { return process != null && process.HasExited; } }
    internal int ExitCode { get { return Exited ? process.ExitCode : int.MinValue; } }
    internal string StartupError { get; private set; }
    internal int ProcessId { get { return process == null ? 0 : process.Id; } }
    internal bool ChildReady { get { return child != IntPtr.Zero; } }
    internal Rect HostBounds { get { return Bounds(handle); } }
    internal Rect GameBounds { get { return Bounds(child); } }
    private static Rect Bounds(IntPtr hwnd)
    {
        RectI bounds;
        return hwnd != IntPtr.Zero && GetWindowRect(hwnd, out bounds)
            ? new Rect(bounds.Left, bounds.Top, bounds.Right - bounds.Left, bounds.Bottom - bounds.Top) : Rect.Empty;
    }
    internal PinballNativeHost(PinballPayload files)
    {
        startInfo = delegate(IntPtr parent) { return CatStartInfo(files, parent); };
        statusPath = files.Status;
    }
    internal PinballNativeHost(ProcessStartInfo info, PokemonPinballEngine engine)
    {
        startInfo = delegate { return info; };
        pokemonEngine = engine;
        Focusable = false;
    }
    protected override HandleRef BuildWindowCore(HandleRef parent)
    {
        handle = CreateWindowEx(0, "static", "", 0x56000004, 0, 0, 440, 860, parent.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        if (handle == IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        try
        {
            job = CreateJobObject(IntPtr.Zero, null);
            if (job == IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            var limits = new JobLimits(); limits.Basic.Flags = 0x2000;
            if (!SetInformationJobObject(job, 9, ref limits, (uint)Marshal.SizeOf(typeof(JobLimits))))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            process = Process.Start(startInfo(handle));
            if (!AssignProcessToJobObject(job, process.Handle)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
        catch (Exception ex) { Stop(); StartupError = ex.Message; }
        return new HandleRef(this, handle);
    }
    private static ProcessStartInfo CatStartInfo(PinballPayload payload, IntPtr parent)
    {
        string log = Path.ChangeExtension(payload.Status, ".log");
        return new ProcessStartInfo(payload.Engine,
            "--main-pack \"" + payload.Pack + "\" --wid " + parent.ToInt64().ToString(CultureInfo.InvariantCulture) +
            " --windowed --resolution 440x860 --position 0,0 --single-window --accessibility disabled --audio-driver Dummy --log-file \"" + log +
            "\" -- --drifft-status \"" + payload.Status + "\"")
        { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, WorkingDirectory = Path.GetDirectoryName(payload.Pack) };

    }
    internal void Poll()
    {
        if (stopping || handle == IntPtr.Zero || process == null || process.HasExited) return;
        if (child == IntPtr.Zero)
        {
            child = GetWindow(handle, 5);
            if (child == IntPtr.Zero)
            {
                EnumWindows(delegate(IntPtr candidate, IntPtr ignored)
                {
                    uint pid; GetWindowThreadProcessId(candidate, out pid);
                    if (pid != process.Id || (pokemonEngine.HasValue && !IsPokemonWindow(candidate, pokemonEngine.Value))) return true;
                    child = candidate; return false;
                }, IntPtr.Zero);
                if (child != IntPtr.Zero)
                {
                    long style = GetStyle(child).ToInt64();
                    long embeddedStyle = (style & ~0x80c40000L) | 0x56000000L;
                    if (pokemonEngine.HasValue) embeddedStyle |= 0x08000000L; // El teclado permanece en la ventana de simulación.
                    SetStyle(child, new IntPtr(embeddedStyle));
                    SetParent(child, handle);
                    if (GetParent(child) != handle) { child = IntPtr.Zero; return; }
                }
            }
            if (child != IntPtr.Zero)
            {
                ShowWindow(child, pokemonEngine.HasValue ? 4 : 5);
                if (!pokemonEngine.HasValue) SetFocus(child);
            }
        }
        if (child != IntPtr.Zero)
        {
            RectI rectangle; GetClientRect(handle, out rectangle);
            Rect hostBounds = HostBounds, gameBounds = GameBounds;
            // Godot puede recolocar la ventana al arrancar, aun manteniendo su tamaño.
            // Reconciliar también su posición evita sumar dos veces el margen centrado.
            bool displaced = hostBounds.IsEmpty || gameBounds.IsEmpty ||
                Math.Abs(gameBounds.Left - hostBounds.Left) > 0.5 || Math.Abs(gameBounds.Top - hostBounds.Top) > 0.5;
            if (displaced || childWidth != rectangle.Right || childHeight != rectangle.Bottom ||
                Math.Abs(gameBounds.Width - rectangle.Right) > 0.5 || Math.Abs(gameBounds.Height - rectangle.Bottom) > 0.5)
            { childWidth = rectangle.Right; childHeight = rectangle.Bottom; MoveWindow(child, 0, 0, childWidth, childHeight, true); }
        }
    }
    internal void Forward(Key input, bool down, bool repeat)
    {
        if (child == IntPtr.Zero || stopping) return;
        Key key = pokemonEngine.HasValue ? PokemonInput(input, pokemonEngine.Value) : input;
        if (pokemonEngine.HasValue) repeat = false;
        key = ChangeKeyState(pressed, input, key, down, ref repeat);
        if (key != Key.None) SendKey(key, down, repeat);
    }
    internal void ReleaseControls()
    {
        if (child != IntPtr.Zero && !stopping)
            foreach (Key key in new HashSet<Key>(pressed.Values)) SendKey(key, false, false);
        pressed.Clear();
    }
    internal static Key ChangeKeyState(Dictionary<Key, Key> keys, Key input, Key mapped, bool down, ref bool repeat)
    {
        if (mapped == Key.None) return Key.None;
        if (!down)
            return keys.Remove(input) && !keys.ContainsValue(mapped) ? mapped : Key.None;
        if (keys.ContainsKey(input)) return repeat ? mapped : Key.None;
        repeat = false;
        bool alreadyHeld = keys.ContainsValue(mapped);
        keys.Add(input, mapped);
        return alreadyHeld ? Key.None : mapped;
    }
    private void SendKey(Key key, bool down, bool repeat)
    {
        int vk = KeyInterop.VirtualKeyFromKey(key), scan = (int)MapVirtualKey((uint)vk, 0);
        if (key == Key.LeftCtrl || key == Key.RightCtrl) { vk = 0x11; scan = 0x1d; }
        long flags = 1 | ((long)scan << 16);
        if (key == Key.RightCtrl || key == Key.Up || key == Key.Down || key == Key.Left || key == Key.Right)
            flags |= 1L << 24;
        if (repeat || !down) flags |= 1L << 30;
        if (!down) flags |= 1L << 31;
        PostMessage(child, down ? 0x0100u : 0x0101u, new IntPtr(vk), new IntPtr(flags));
    }
    internal static Key PokemonInput(Key key, PokemonPinballEngine engine)
    {
        if (key == Key.LeftCtrl) return engine == PokemonPinballEngine.Mgba ? Key.Left :
            engine == PokemonPinballEngine.JvppbPort ? Key.LeftCtrl : Key.Z;
        if (key == Key.RightCtrl) return engine == PokemonPinballEngine.JvppbPort ? Key.RightCtrl : Key.X;
        if (engine == PokemonPinballEngine.JvppbPort)
        {
            if (key == Key.P || key == Key.Back) return Key.Escape;
        }
        else
        {
            if (key == Key.Enter) return Key.X;
            if (key == Key.P) return Key.Enter;
            if (engine == PokemonPinballEngine.Mgba)
            {
                if (key == Key.Space) return Key.X;
                if (key == Key.Back) return Key.Z;
                if (key == Key.Tab) return Key.Back; // Select.
            }
        }
        switch (key)
        {
            case Key.Space: case Key.Enter: case Key.Up: case Key.Down: case Key.Left:
            case Key.Right: case Key.C: case Key.V: case Key.T: case Key.X: case Key.Z: case Key.A: case Key.S: case Key.Back: case Key.Tab:
            case Key.F5: case Key.F8: case Key.F9: case Key.F10: return key;
            default: return Key.None;
        }
    }
    private static bool IsPokemonWindow(IntPtr hwnd, PokemonPinballEngine engine)
    {
        var name = new StringBuilder(32);
        GetClassName(hwnd, name, name.Capacity);
        return name.ToString() == (engine == PokemonPinballEngine.PinballNs ? "PinballNS" : "SDL_app");
    }
    internal void Stop()
    {
        if (stopping) return; stopping = true;
        Process current = process;
        IntPtr currentJob = job;
        process = null;
        job = IntPtr.Zero;
        if (pokemonEngine.HasValue && child != IntPtr.Zero && current != null &&
            PostMessage(child, 0x0010, IntPtr.Zero, IntPtr.Zero))
        {
            // Deja guardar la partida al proceso hijo sin bloquear la interfaz.
            Task.Run(delegate { StopProcess(current, currentJob, 1000); });
        }
        else StopProcess(current, currentJob, 0);
        try { if (File.Exists(statusPath)) File.Delete(statusPath); } catch (IOException) { }
    }
    private static void StopProcess(Process current, IntPtr currentJob, int gracefulMilliseconds)
    {
        try
        {
            if (current != null && !current.WaitForExit(gracefulMilliseconds)) current.Kill();
        }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
        finally
        {
            if (current != null) current.Dispose();
            if (currentJob != IntPtr.Zero) CloseHandle(currentJob);
        }
    }
    protected override void DestroyWindowCore(HandleRef hwnd) { Stop(); DestroyWindow(hwnd.Handle); handle = child = IntPtr.Zero; }
    [StructLayout(LayoutKind.Sequential)] private struct RectI { internal int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct JobBasic
    {
        internal long ProcessTime, JobTime; internal uint Flags; internal UIntPtr MinWorkingSet, MaxWorkingSet;
        internal uint ActiveProcesses; internal UIntPtr Affinity; internal uint Priority, Scheduling;
    }
    [StructLayout(LayoutKind.Sequential)] private struct JobIo { internal ulong ReadOps, WriteOps, OtherOps, ReadBytes, WriteBytes, OtherBytes; }
    [StructLayout(LayoutKind.Sequential)] private struct JobLimits
    { internal JobBasic Basic; internal JobIo Io; internal UIntPtr ProcessMemory, JobMemory, PeakProcessMemory, PeakJobMemory; }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr CreateJobObject(IntPtr security, string name);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetInformationJobObject(IntPtr job, int kind, ref JobLimits limits, uint size);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr CreateWindowEx(int ex, string cls, string title, int style, int x, int y, int w, int h, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr parameter);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr hwnd, StringBuilder name, int length);
    [DllImport("user32.dll")] private static extern bool DestroyWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr hwnd, uint command);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hwnd, out RectI rectangle);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out RectI rectangle);
    [DllImport("user32.dll")] private static extern bool MoveWindow(IntPtr hwnd, int x, int y, int width, int height, bool repaint);
    [DllImport("user32.dll")] private static extern IntPtr SetFocus(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hwnd, uint msg, IntPtr wp, IntPtr lp);
    [DllImport("user32.dll")] private static extern uint MapVirtualKey(uint code, uint type);
    private delegate bool EnumWindowProc(IntPtr hwnd, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowProc callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint process);
    [DllImport("user32.dll")] private static extern IntPtr SetParent(IntPtr child, IntPtr parent);
    [DllImport("user32.dll")] private static extern IntPtr GetParent(IntPtr child);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hwnd, int command);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetLong64(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetLong32(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern IntPtr SetLong64(IntPtr hwnd, int index, IntPtr value);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")] private static extern int SetLong32(IntPtr hwnd, int index, int value);
    private static IntPtr GetStyle(IntPtr hwnd) { return IntPtr.Size == 8 ? GetLong64(hwnd, -16) : new IntPtr(GetLong32(hwnd, -16)); }
    private static void SetStyle(IntPtr hwnd, IntPtr style) { if (IntPtr.Size == 8) SetLong64(hwnd, -16, style); else SetLong32(hwnd, -16, style.ToInt32()); }
}
