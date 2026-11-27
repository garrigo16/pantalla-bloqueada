using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

// Autor: Miquel Garrigo
internal static class Program
{
    internal const string InstanceName = "Local\\PantallaBloqueada.Residente.v2";
    internal const string ToggleName = "Local\\PantallaBloqueada.Alternar.v2";
    internal const string QuitName = "Local\\PantallaBloqueada.Cerrar.v2";
    internal const string ConfigName = "Local\\PantallaBloqueada.Configurar.v2";

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 1 && LidPowerSession.IsWorker(args[0])) return LidPowerSession.RunWorker(args[0]);
        bool ownsMutex = false;
        Mutex mutex = null;
        try
        {
            Native.SetProcessDPIAware();
            bool quit = Array.IndexOf(args, "--quit") >= 0;
            bool toggle = Array.IndexOf(args, "--toggle") >= 0;
            bool configure = args.Length == 0 || Array.IndexOf(args, "--config") >= 0;
            mutex = new Mutex(true, InstanceName, out ownsMutex);
            if (!ownsMutex)
            {
                if (quit || toggle || configure)
                {
                    using (var signal = EventWaitHandle.OpenExisting(quit ? QuitName : (toggle ? ToggleName : ConfigName)))
                        signal.Set();
                }
                return 0;
            }
            if (quit) return 0;
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            using (var controller = new LockController(app))
            {
                if (toggle) app.Dispatcher.BeginInvoke(new Action(controller.Toggle));
                if (configure) app.Dispatcher.BeginInvoke(new Action(controller.OpenSettings));
                return app.Run();
            }
        }
        catch (WaitHandleCannotBeOpenedException) { return 1; }
        catch (Exception ex)
        {
            UpdateLog.Write("app-inicio", ex);
            MessageBox.Show("No se pudo activar la aplicación.\n\n" + ex.Message,
                AppIdentity.DisplayName, MessageBoxButton.OK, MessageBoxImage.Information);
            return 1;
        }
        finally
        {
            if (mutex != null)
            {
                if (ownsMutex) mutex.ReleaseMutex();
                mutex.Dispose();
            }
        }
    }
}

internal static class Native
{
    internal const int WmHotkey = 0x0312;
    [DllImport("user32.dll")] internal static extern bool SetProcessDPIAware();
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr hwnd, IntPtr process);
    [DllImport("user32.dll")] internal static extern IntPtr GetKeyboardLayout(uint thread);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern short VkKeyScanEx(char key, IntPtr layout);
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern uint SetThreadExecutionState(uint flags);
    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
}

internal sealed partial class LockController : IDisposable
{
    private readonly Application app;
    private HwndSource source;
    private Forms.NotifyIcon tray;
    private UpdateManager updates;
    private IdleActivationMonitor idle;
    private Forms.ToolStripMenuItem updateMenu;
    private EventWaitHandle toggleEvent;
    private EventWaitHandle quitEvent;
    private RegisteredWaitHandle toggleWait;
    private RegisteredWaitHandle quitWait;
    private EventWaitHandle configEvent;
    private RegisteredWaitHandle configWait;
    private readonly List<LockWindow> windows = new List<LockWindow>();
    private bool closing;
    private bool disposed;
    private bool keepingAwake;
    private bool activating;
    private bool sessionLocked;
    private AppSettings settings;
    private LidPowerSession lidPower;
    private string lidPowerError;
    private SettingsWindow configuration;
    private readonly Stopwatch elapsed = new Stopwatch();
    private readonly DispatcherTimer blackout = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
    private readonly DispatcherTimer activityTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
    private double nextActivity;
    private int activityDirection = -1;
    internal string LastActivityError { get; private set; }
    internal bool KeepingAwake { get { return keepingAwake; } }
    internal IntPtr Handle { get { return source.Handle; } }
    internal IList<LockWindow> Windows { get { return windows.AsReadOnly(); } }

    internal LockController(Application application, AppSettings preferences = null)
    {
        app = application;
        settings = preferences ?? AppSettings.Load();
        InitializeFun();
        try
        {
            var parameters = new HwndSourceParameters("DrifftAnalyzer.Controller");
            parameters.ParentWindow = new IntPtr(-3);
            source = new HwndSource(parameters);
            source.AddHook(WindowMessage);
            InitializeHotkeys();

            toggleEvent = new EventWaitHandle(false, EventResetMode.AutoReset, Program.ToggleName);
            quitEvent = new EventWaitHandle(false, EventResetMode.AutoReset, Program.QuitName);
            configEvent = new EventWaitHandle(false, EventResetMode.AutoReset, Program.ConfigName);
            toggleWait = ThreadPool.RegisterWaitForSingleObject(toggleEvent,
                delegate { Dispatch(Toggle); }, null, Timeout.Infinite, false);
            quitWait = ThreadPool.RegisterWaitForSingleObject(quitEvent,
                delegate { Dispatch(delegate { app.Shutdown(); }); }, null, Timeout.Infinite, false);
            configWait = ThreadPool.RegisterWaitForSingleObject(configEvent,
                delegate { Dispatch(OpenSettings); }, null, Timeout.Infinite, false);

            tray = new Forms.NotifyIcon
            {
                Icon = AppIcon.Tray,
                Text = AppIdentity.DisplayName,
                Visible = true
            };
            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add("Configurar…", null, delegate { OpenSettings(); });
            updates = new UpdateManager();
            updateMenu = new Forms.ToolStripMenuItem("Buscar actualizaciones");
            updateMenu.Click += async delegate { OpenSettings(); if (updates.Available == null) await updates.CheckAsync(); };
            menu.Items.Add(updateMenu);
            updates.Changed += UpdatesChanged;
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("Cerrar aplicación", null, delegate { app.Shutdown(); });
            tray.ContextMenuStrip = menu;
            tray.DoubleClick += delegate { OpenSettings(); };
            blackout.Tick += delegate
            {
                if (settings.TimeoutSeconds > 0 && elapsed.Elapsed.TotalSeconds >= settings.TimeoutSeconds)
                {
                    foreach (var window in windows) window.Darken();
                    blackout.Stop();
                }
            };
            activityTimer.Tick += delegate
            {
                if (elapsed.Elapsed.TotalSeconds < nextActivity) return;
                try
                {
                    activityDirection = -Activity.MoveOnePixel(activityDirection);
                    var funWindow = FunWindow();
                    if (funWindow != null) funWindow.React(fun.Pixel(!funWindow.IsDark), null);
                    nextActivity = elapsed.Elapsed.TotalSeconds + settings.ActivityIntervalSeconds;
                }
                catch (Exception ex)
                {
                    LastActivityError = ex.Message;
                    Hide();
                    tray.ShowBalloonTip(5000, AppIdentity.DisplayName, ex.Message, Forms.ToolTipIcon.Warning);
                }
            };
            SystemEvents.DisplaySettingsChanged += DisplaySettingsChanged;
            SystemEvents.SessionSwitch += SessionChanged;
            UpdatesChanged(this, EventArgs.Empty);
            updates.Start();
            idle = new IdleActivationMonitor(new SystemIdleInput(), delegate { return settings; },
                delegate { return HotkeyRegistered && !disposed && !closing && !activating && !sessionLocked && windows.Count == 0 && configuration == null && updates.Phase != UpdatePhase.Installing; },
                delegate
                {
                    try { Show(); }
                    catch { tray.ShowBalloonTip(5000, AppIdentity.DisplayName, "No se pudo activar la simulación. Puedes volver a intentarlo manualmente.", Forms.ToolTipIcon.Warning); }
                });
            idle.Start();
            try { SetLidPower(settings.KeepRunningWithLidClosed); }
            catch (Exception ex) { ReportLidPowerError(ex); }
            if (hotkeyError != null) Dispatch(OpenSettings);
        }
        catch { Dispose(); throw; }
    }

    private void Dispatch(Action action)
    {
        if (!disposed && !app.Dispatcher.HasShutdownStarted)
            app.Dispatcher.BeginInvoke(new Action(delegate { if (!disposed) action(); }));
    }

    private IntPtr WindowMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == Native.WmHotkey && hotkeys != null)
        {
            var action = hotkeys.ActionFor(wParam.ToInt32());
            handled = action != ShortcutAction.None;
            HandleShortcut(action);
        }
        return IntPtr.Zero;
    }

    private void HandleShortcut(ShortcutAction action)
    {
        if (action == ShortcutAction.Toggle) Toggle();
        else if (action == ShortcutAction.Activate) Show();
        else if (action == ShortcutAction.Unlock) Hide();
        else if (action == ShortcutAction.Game) OpenPinball();
    }

    internal void Toggle()
    {
        if (disposed) return;
        if (windows.Count > 0) Hide();
        else Show();
    }

    private void Show(bool resetFun = true)
    {
        if (disposed || activating || windows.Count > 0) return;
        if (!HotkeyRegistered) { OpenSettings(); return; }
        activating = true;
        try
        {
            if (resetFun) fun.Reset();
            lastPointer = null;
            if (configuration != null) configuration.Close();
            var screens = Forms.Screen.AllScreens;
            var selected = Forms.Screen.PrimaryScreen;
            foreach (var screen in screens)
                if (screen.DeviceName == settings.DeviceName) selected = screen;
            if (settings.KeepAwake)
            {
                if (Native.SetThreadExecutionState(0x80000003) == 0)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "No se pudo mantener activo el equipo.");
                keepingAwake = true;
            }
            // Primero las pantallas negras; la elegida queda enfocada al final.
            foreach (var screen in screens)
                if (screen.DeviceName != selected.DeviceName) AddWindow(screen, false);
            AddWindow(selected, true);
            windows[windows.Count - 1].Activate();
            StartSimulationKeyboard();
            elapsed.Restart();
            if (settings.SimulateActivity)
            {
                LastActivityError = null;
                activityDirection = -Activity.MoveOnePixel(-1);
                nextActivity = settings.ActivityIntervalSeconds;
                activityTimer.Start();
            }
            if (settings.TimeoutSeconds > 0) blackout.Start();
        }
        catch { Hide(); throw; }
        finally { activating = false; }
    }

    private void AddWindow(Forms.Screen screen, bool showImage)
    {
        var window = new LockWindow(screen, showImage, settings);
        window.KeyPressed += FunKeyDown;
        window.KeyReleased += FunKeyUp;
        window.TextEntered += FunTextInput;
        window.MousePressed += FunMouseDown;
        window.PointerMoved += FunMouseMove;
        windows.Add(window);
        window.Closed += delegate { if (!closing) Hide(); };
        window.Show();
    }

    private void DisplaySettingsChanged(object sender, EventArgs e)
    {
        Dispatch(delegate { if (windows.Count > 0) { Hide(); Show(false); } });
    }

    private void SessionChanged(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionLock || e.Reason == SessionSwitchReason.SessionLogoff ||
            e.Reason == SessionSwitchReason.ConsoleDisconnect || e.Reason == SessionSwitchReason.RemoteDisconnect)
            Dispatch(delegate { sessionLocked = true; Hide(); });
        else if (e.Reason == SessionSwitchReason.SessionUnlock || e.Reason == SessionSwitchReason.SessionLogon ||
            e.Reason == SessionSwitchReason.ConsoleConnect || e.Reason == SessionSwitchReason.RemoteConnect)
            Dispatch(delegate { sessionLocked = false; if (idle != null) idle.Reset(); });
    }

    internal void OpenSettings()
    {
        if (windows.Count != 0) return;
        if (configuration != null) { configuration.Activate(); return; }
        configuration = new SettingsWindow(settings, updates, fun.Stats, SavePreferences, lidPowerError);
        configuration.Saved += delegate(AppSettings updated) { settings = updated; if (idle != null) idle.Reset(); };
        configuration.Closed += delegate { configuration = null; };
        configuration.Show();
        if (hotkeyError != null) configuration.ShowShortcutError(hotkeyError);
    }

    private void SetLidPower(bool enabled)
    {
        if (!enabled && lidPower != null)
        {
            var previous = lidPower;
            lidPower = null;
            previous.Stop();
        }
        if (lidPower == null) lidPower = LidPowerSession.Start(enabled);
        lidPowerError = null;
    }

    private void ReportLidPowerError(Exception ex)
    {
        UpdateLog.Write("ajuste-tapa", ex);
        lidPowerError = ex.Message;
        if (tray != null)
            tray.ShowBalloonTip(6000, AppIdentity.DisplayName, "Ajuste de cierre de tapa. " + lidPowerError, Forms.ToolTipIcon.Warning);
    }

    private void UpdatesChanged(object sender, EventArgs e)
    {
        if (updates == null || tray == null) return;
        string text = AppIdentity.DisplayName + " · " + ProductVersion.Current;
        if (updates.Available != null) text += " · actualización " + updates.Available.Version;
        tray.Text = text.Length > 63 ? text.Substring(0, 63) : text;
        updateMenu.Text = updates.Available == null ? "Buscar actualizaciones" : "Actualización disponible: " + updates.Available.Version;
        updateMenu.Enabled = !updates.IsBusy;
    }

    internal void Hide()
    {
        if (closing) return;
        bool wasShowing = activating || windows.Count > 0;
        closing = true;
        try
        {
            StopSimulationKeyboard();
            blackout.Stop();
            activityTimer.Stop();
            elapsed.Reset();
            if (keepingAwake) { Native.SetThreadExecutionState(0x80000000); keepingAwake = false; }
            foreach (var window in windows) window.Close();
            windows.Clear();
        }
        finally { closing = false; if (wasShowing && idle != null) idle.DeferUntilActivity(); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        SystemEvents.DisplaySettingsChanged -= DisplaySettingsChanged;
        SystemEvents.SessionSwitch -= SessionChanged;
        if (idle != null) idle.Dispose();
        if (updates != null) { updates.Changed -= UpdatesChanged; updates.Dispose(); }
        if (configuration != null) configuration.Close();
        Hide();
        try { SetLidPower(false); }
        catch (Exception ex) { ReportLidPowerError(ex); }
        if (toggleWait != null) toggleWait.Unregister(null);
        if (quitWait != null) quitWait.Unregister(null);
        if (configWait != null) configWait.Unregister(null);
        if (toggleEvent != null) toggleEvent.Dispose();
        if (quitEvent != null) quitEvent.Dispose();
        if (configEvent != null) configEvent.Dispose();
        if (tray != null) { tray.Visible = false; tray.Dispose(); }
        if (source != null)
        {
            if (hotkeys != null) hotkeys.Dispose();
            source.Dispose();
        }
    }
}

internal sealed partial class LockWindow : Window
{
    private TextBlock clock;
    private TextBlock date;
    private DispatcherTimer timer;
    internal readonly Forms.Screen Screen;
    internal readonly Grid Surface;
    internal readonly bool HasImage;
    private Grid imageLayer;
    internal bool IsDark { get { return (imageLayer == null || imageLayer.Visibility != Visibility.Visible) &&
        (lunarScene == null || lunarScene.Visibility != Visibility.Visible); } }
    private readonly CultureInfo culture = CultureInfo.GetCultureInfo("es-ES");

    internal LockWindow(Forms.Screen screen, bool showImage, AppSettings settings)
    {
        Screen = screen;
        preferences = settings;
        HasImage = showImage;
        Title = AppIdentity.DisplayName;
        Icon = AppIcon.Window;
        WindowStartupLocation = WindowStartupLocation.Manual;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;
        ShowInTaskbar = false;
        Cursor = Cursors.None;
        Background = Brushes.Black;
        UseLayoutRounding = true;
        Surface = new Grid { Background = Brushes.Black, ClipToBounds = true };
        Content = Surface;
        if (showImage)
        {
            BuildLockImage(settings);
            if (settings.Theme == AppTheme.Pokemon && settings.TimeoutSeconds > 0) PrepareLunarScene();
        }
        InitializeFunWindow();

        // El atajo de salida no se anuncia en la superficie.
        SourceInitialized += delegate
        {
            var hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            hwndSource.AddHook(delegate(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                int command = (int)(wParam.ToInt64() & 0xfff0);
                if (settings.KeepAwake && message == 0x0112 && (command == 0xf140 || command == 0xf170))
                    handled = true;
                return IntPtr.Zero;
            });
            var rect = screen.Bounds;
            if (!Native.SetWindowPos(new WindowInteropHelper(this).Handle, new IntPtr(-1),
                rect.X, rect.Y, rect.Width, rect.Height, 0x0040))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        };
        Closed += delegate { if (timer != null) timer.Stop(); StopLunarScene(); DisposeFunWindow(); };
    }

    internal void Darken()
    {
        darkened = true;
        peekTimer.Stop();
        if (imageLayer != null) imageLayer.Visibility = Visibility.Collapsed;
        if (timer != null) timer.Stop();
        ScheduleLunarScene();
    }

    private void BuildLockImage(AppSettings settings)
    {
        imageLayer = new Grid { Background = new LinearGradientBrush(Color.FromRgb(23, 77, 206), Color.FromRgb(2, 12, 53), 65) };
        Surface.Children.Add(imageLayer);
        string custom = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fondo.jpg");
        string stock = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Web", "Screen", "img100.jpg");
        string wallpaper = File.Exists(settings.ImagePath) ? settings.ImagePath : (File.Exists(custom) ? custom : stock);
        if (File.Exists(wallpaper))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(wallpaper);
                bitmap.EndInit();
                bitmap.Freeze();
                imageLayer.Background = new ImageBrush(bitmap) { Stretch = Stretch.UniformToFill };
            }
            catch { /* Mantener el fondo azul si no se puede leer la imagen. */ }
        }
        imageLayer.Children.Add(new Border { Background = new SolidColorBrush(Color.FromArgb(22, 0, 0, 0)), IsHitTestVisible = false });
        var canvas = new Grid { Width = 1920, Height = 1080 };
        imageLayer.Children.Add(new Viewbox { Stretch = Stretch.Uniform, Child = canvas });
        var timePanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 155, 0, 0),
            Effect = new DropShadowEffect { BlurRadius = 22, ShadowDepth = 2, Opacity = 0.22 }
        };
        timePanelElement = timePanel;
        clock = new TextBlock
        {
            FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI"),
            FontWeight = FontWeights.SemiBold, FontSize = 120, Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        date = new TextBlock
        {
            FontFamily = new FontFamily("Segoe UI"), FontWeight = FontWeights.Medium,
            FontSize = 27, Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, -1, 0, 0)
        };
        timePanel.Children.Add(clock);
        timePanel.Children.Add(date);
        canvas.Children.Add(timePanel);
        timePanel.Visibility = settings.ShowClock ? Visibility.Visible : Visibility.Collapsed;
        var network = new TextBlock
        {
            FontFamily = new FontFamily("Segoe MDL2 Assets"), Text = "\uE839", FontSize = 24,
            Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 45, 40),
            Opacity = 0.9, IsHitTestVisible = false
        };
        networkIconElement = network;
        canvas.Children.Add(network);
        timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += delegate { UpdateClock(); };
        UpdateClock();
        timer.Start();
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        clock.Text = now.ToString("HH:mm", culture);
        date.Text = now.ToString("dddd, d 'de' MMMM", culture) + EasterEggEngine.CalendarSuffix(now, preferences.Humor);
    }
}
