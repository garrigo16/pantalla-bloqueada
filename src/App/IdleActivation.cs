using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Threading;

// Autor: Miquel Garrigo
internal struct IdleSample
{
    internal readonly uint LastInputTick;
    internal readonly uint IdleMilliseconds;
    internal IdleSample(uint lastInputTick, uint idleMilliseconds)
    { LastInputTick = lastInputTick; IdleMilliseconds = idleMilliseconds; }
}

internal interface IIdleInputSource
{
    bool TryRead(out IdleSample sample);
}

internal sealed class SystemIdleInput : IIdleInputSource
{
    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo { internal uint Size, Time; }
    [DllImport("user32.dll")] private static extern bool GetLastInputInfo(ref LastInputInfo info);
    [DllImport("user32.dll")] private static extern IntPtr OpenInputDesktop(uint flags, bool inherit, uint access);
    [DllImport("user32.dll")] private static extern bool CloseDesktop(IntPtr desktop);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetUserObjectInformation(IntPtr handle, int index, StringBuilder value, uint bytes, out uint needed);

    internal static uint ElapsedMilliseconds(uint now, uint lastInput)
    {
        uint elapsed = unchecked(now - lastInput);
        // SendInput puede suministrar una marca futura. No interpretarla como días de inactividad.
        return elapsed > int.MaxValue ? 0 : elapsed;
    }

    public bool TryRead(out IdleSample sample)
    {
        sample = new IdleSample();
        IntPtr desktop = OpenInputDesktop(0, false, 0x0001);
        if (desktop == IntPtr.Zero) return false;
        try
        {
            var name = new StringBuilder(128);
            uint needed;
            if (!GetUserObjectInformation(desktop, 2, name, (uint)name.Capacity * 2, out needed) ||
                !string.Equals(name.ToString(), "Default", StringComparison.OrdinalIgnoreCase)) return false;
            var info = new LastInputInfo { Size = (uint)Marshal.SizeOf(typeof(LastInputInfo)) };
            if (!GetLastInputInfo(ref info)) return false;
            sample = new IdleSample(info.Time, ElapsedMilliseconds(unchecked((uint)Environment.TickCount), info.Time));
            return true;
        }
        finally { CloseDesktop(desktop); }
    }
}

internal sealed class IdleActivationPolicy
{
    private bool deferred;
    private uint? deferredInput;
    internal void Reset() { deferred = false; deferredInput = null; }
    internal void DeferUntilActivity(IdleSample? sample)
    {
        deferred = true;
        deferredInput = sample.HasValue ? (uint?)sample.Value.LastInputTick : null;
    }
    internal bool ShouldActivate(IdleSample sample, int seconds)
    {
        if (seconds < 1 || seconds > 86400) return false;
        if (deferred)
        {
            if (!deferredInput.HasValue) { deferredInput = sample.LastInputTick; return false; }
            if (sample.LastInputTick == deferredInput.Value) return false;
            Reset();
        }
        return sample.IdleMilliseconds >= (ulong)seconds * 1000;
    }
}

internal sealed class IdleActivationMonitor : IDisposable
{
    private readonly IIdleInputSource input;
    private readonly Func<AppSettings> settings;
    private readonly Func<bool> canActivate;
    private readonly Action activate;
    private readonly IdleActivationPolicy policy = new IdleActivationPolicy();
    private DispatcherTimer timer;
    private bool disposed;

    internal IdleActivationMonitor(IIdleInputSource source, Func<AppSettings> preferences, Func<bool> eligible, Action show)
    { input = source; settings = preferences; canActivate = eligible; activate = show; }

    internal void Start()
    {
        if (disposed || timer != null) return;
        timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += delegate { Poll(); };
        timer.Start();
    }
    internal void Poll()
    {
        if (disposed) return;
        var preferences = settings();
        if (!preferences.AutoActivate || !canActivate()) return;
        IdleSample sample;
        if (input.TryRead(out sample) && policy.ShouldActivate(sample, preferences.IdleSeconds))
        {
            policy.DeferUntilActivity(sample);
            activate();
        }
    }
    internal void DeferUntilActivity()
    {
        if (disposed) return;
        IdleSample sample;
        policy.DeferUntilActivity(input.TryRead(out sample) ? (IdleSample?)sample : null);
    }
    internal void Reset() { policy.Reset(); }
    public void Dispose() { disposed = true; if (timer != null) timer.Stop(); }
}
