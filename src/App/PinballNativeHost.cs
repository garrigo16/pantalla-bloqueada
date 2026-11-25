using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

// Autor: Miquel Garrigo
internal sealed class PinballNativeHost : HwndHost
{
    private readonly PinballPayload payload;
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
    internal PinballNativeHost(PinballPayload files) { payload = files; }
    protected override HandleRef BuildWindowCore(HandleRef parent)
    {
        handle = CreateWindowEx(0, "static", "", 0x56000004, 0, 0, 440, 860, parent.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        if (handle == IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        string log = Path.ChangeExtension(payload.Status, ".log");
        var info = new ProcessStartInfo(payload.Engine,
            "--main-pack \"" + payload.Pack + "\" --wid " + handle.ToInt64().ToString(CultureInfo.InvariantCulture) +
            " --windowed --resolution 440x860 --position 0,0 --single-window --accessibility disabled --audio-driver Dummy --log-file \"" + log +
            "\" -- --drifft-status \"" + payload.Status + "\"")
        { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, WorkingDirectory = Path.GetDirectoryName(payload.Pack) };
        try
        {
            job = CreateJobObject(IntPtr.Zero, null);
            if (job == IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            var limits = new JobLimits(); limits.Basic.Flags = 0x2000;
            if (!SetInformationJobObject(job, 9, ref limits, (uint)Marshal.SizeOf(typeof(JobLimits))))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            process = Process.Start(info);
            if (!AssignProcessToJobObject(job, process.Handle)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
        catch (Exception ex) { Stop(); StartupError = ex.Message; }
        return new HandleRef(this, handle);
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
                    if (pid != process.Id) return true;
                    child = candidate; return false;
                }, IntPtr.Zero);
                if (child != IntPtr.Zero)
                {
                    long style = GetStyle(child).ToInt64();
                    SetStyle(child, new IntPtr((style & ~0x80c40000L) | 0x56000000L));
                    SetParent(child, handle);
                    if (GetParent(child) != handle) { child = IntPtr.Zero; return; }
                }
            }
            if (child != IntPtr.Zero) { ShowWindow(child, 5); SetFocus(child); }
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
    internal void Forward(Key key, bool down, bool repeat)
    {
        if (child == IntPtr.Zero || stopping) return;
        int vk = KeyInterop.VirtualKeyFromKey(key), scan = (int)MapVirtualKey((uint)vk, 0);
        if (key == Key.LeftCtrl || key == Key.RightCtrl) { vk = 0x11; scan = 0x1d; }
        long flags = 1 | ((long)scan << 16);
        if (key == Key.RightCtrl) flags |= 1L << 24;
        if (repeat || !down) flags |= 1L << 30;
        if (!down) flags |= 1L << 31;
        PostMessage(child, down ? 0x0100u : 0x0101u, new IntPtr(vk), new IntPtr(flags));
    }
    internal void Stop()
    {
        if (stopping) return; stopping = true;
        if (process != null)
        {
            try { if (!process.HasExited) process.Kill(); } catch (InvalidOperationException) { } catch (System.ComponentModel.Win32Exception) { }
            process.Dispose(); process = null;
        }
        if (job != IntPtr.Zero) { CloseHandle(job); job = IntPtr.Zero; }
        try { if (File.Exists(payload.Status)) File.Delete(payload.Status); } catch (IOException) { }
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
