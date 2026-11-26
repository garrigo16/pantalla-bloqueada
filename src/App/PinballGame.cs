using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

// Autor: Miquel Garrigo
internal sealed class PinballGame : IDisposable
{
    private readonly Grid surface, layer, slot;
    private readonly Action closed;
    private readonly TextBlock message;
    private readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
    private readonly CancellationTokenSource setup = new CancellationTokenSource();
    private readonly HashSet<Key> heldKeys = new HashSet<Key>();
    private readonly AppSettings settings;
    private string gameName;
    private StackPanel chooser;
    private Button firstChoice, secondChoice;
    private PokemonPinballTitle selected;
    private double gameWidth = 440, gameHeight = 860;
    private bool disposed;
    private DateTime nativeStarted;
    internal PinballNativeHost Host { get; private set; }
    internal bool Loading { get; private set; }
    internal PinballGame(Grid root, AppSettings settings, Action onClosed)
    {
        surface = root; closed = onClosed;
        this.settings = settings;
        layer = new Grid { Background = new SolidColorBrush(Color.FromRgb(7, 12, 18)) };
        Panel.SetZIndex(layer, 200);
        slot = new Grid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        message = new TextBlock { Foreground = Brushes.Bisque, FontSize = 18, Visibility = Visibility.Collapsed,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxWidth = 380 };
        layer.Children.Add(slot); layer.Children.Add(message);
        surface.Children.Add(layer); layer.SizeChanged += Resize; Resize(null, null);
        timer.Tick += delegate
        {
            if (disposed || Host == null) return;
            if (Host.StartupError != null) { Fail("No se pudo iniciar el motor del juego."); return; }
            if (Host.Exited)
            { if (Host.ExitCode == 0) Close(); else Fail(gameName + " no pudo continuar. Puedes volver a intentarlo."); return; }
            Host.Poll();
            if (Host.ChildReady) message.Visibility = Visibility.Collapsed;
            else if (DateTime.UtcNow - nativeStarted > TimeSpan.FromSeconds(30)) Fail("El juego ha tardado demasiado en arrancar.");
        };
        timer.Start();
        if (settings.Theme == AppTheme.Pokemon) ShowChooser();
        else StartGame(PokemonPinballTitle.Original);
    }

    private void ShowChooser()
    {
        chooser = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            Width = 410, Margin = new Thickness(24), Cursor = Cursors.Arrow };
        chooser.Children.Add(new TextBlock { Text = "Elige el pinball", Foreground = Brushes.Bisque,
            FontSize = 29, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 16) });
        firstChoice = AddChoice(PokemonPinballTitle.Original, "Game Boy Color");
        secondChoice = AddChoice(PokemonPinballTitle.RubySapphire, "Game Boy Advance");
        var back = new Button { Content = "Volver", HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 5, 0, 0) };
        back.Click += delegate { Close(); };
        chooser.Children.Add(back);
        Select(PokemonPinballTitle.Original);
        layer.Children.Add(chooser);
    }

    private Button AddChoice(PokemonPinballTitle game, string system)
    {
        var contents = new StackPanel();
        contents.Children.Add(new TextBlock { Text = PokemonPinballRom.Title(game), FontSize = 19, FontWeight = FontWeights.SemiBold });
        contents.Children.Add(new TextBlock { Text = system, FontSize = 14, Margin = new Thickness(0, 4, 0, 0) });
        var button = new Button { Content = contents, Padding = new Thickness(14, 10, 14, 10),
            HorizontalContentAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 9) };
        button.Click += delegate { StartGame(game); };
        chooser.Children.Add(button);
        return button;
    }

    private void Select(PokemonPinballTitle game)
    {
        selected = game;
        var active = new SolidColorBrush(Color.FromRgb(178, 219, 209));
        var normal = new SolidColorBrush(Color.FromRgb(212, 217, 226));
        firstChoice.Background = game == PokemonPinballTitle.Original ? active : normal;
        secondChoice.Background = game == PokemonPinballTitle.RubySapphire ? active : normal;
    }

    private void StartGame(PokemonPinballTitle game)
    {
        bool pokemon = settings.Theme == AppTheme.Pokemon;
        if (pokemon)
        {
            if (chooser == null) return;
            layer.Children.Remove(chooser);
            chooser = null;
        }
        Loading = true;
        gameName = pokemon ? PokemonPinballRom.Title(game) : "Cat Pinball";
        message.Text = pokemon ? "Preparando " + gameName + "…" : "Preparando el rincón del gato…";
        message.Visibility = Visibility.Visible;
        CancellationToken cancellation = setup.Token;
        string rom = game == PokemonPinballTitle.Original ? settings.PokemonRomPath : settings.PokemonRubyRomPath;
        PokemonPinballEngine? engine = null;
        Task.Run(async delegate
        {
            if (!pokemon)
            {
                PinballPayload payload = PinballPayload.Prepare();
                return new Func<PinballNativeHost>(delegate { return new PinballNativeHost(payload); });
            }
            engine = PokemonPinballRom.Validate(rom, game);
            var info = engine == PokemonPinballEngine.PinballNs
                ? (await PinballNsPayload.PrepareAsync(rom, cancellation).ConfigureAwait(false)).StartInfo()
                : await PokemonPinballProcess.PrepareAsync(engine.Value, rom, cancellation).ConfigureAwait(false);
            return new Func<PinballNativeHost>(delegate { return new PinballNativeHost(info, engine.Value); });
        }, cancellation).ContinueWith(delegate(Task<Func<PinballNativeHost>> task)
        {
            Exception error = task.IsFaulted ? task.Exception.GetBaseException() : null;
            if (task.IsCanceled || surface.Dispatcher.HasShutdownStarted) return;
            surface.Dispatcher.BeginInvoke(new Action(delegate
            {
                if (disposed) return; Loading = false;
                if (error != null) { Fail("No se pudo preparar " + gameName + ".\n" + error.Message); return; }
                try
                {
                    if (engine == PokemonPinballEngine.Mgba)
                    {
                        gameWidth = game == PokemonPinballTitle.Original ? 600 : 660;
                        gameHeight = game == PokemonPinballTitle.Original ? 540 : 440;
                        Resize(null, null);
                    }
                    Host = task.Result();
                    nativeStarted = DateTime.UtcNow;
                    slot.Children.Add(Host);
                }
                catch (Exception ex) { Fail("No se pudo abrir " + gameName + ".\n" + ex.Message); }
            }));
        }, TaskScheduler.Default);
    }
    private void Resize(object sender, SizeChangedEventArgs args)
    {
        double scale = Math.Max(0.1, Math.Min(1.5, Math.Min(Math.Max(1, layer.ActualWidth - 36) / gameWidth, Math.Max(1, layer.ActualHeight - 36) / gameHeight)));
        slot.Width = gameWidth * scale; slot.Height = gameHeight * scale;
    }
    private void Fail(string text)
    {
        if (Host != null) { Host.Dispose(); Host = null; slot.Children.Clear(); }
        message.Text = text + "\nEsc para volver."; message.Visibility = Visibility.Visible;
    }
    internal void Key(Key key, bool down, bool repeat)
    {
        if (disposed) return;
        if (key == System.Windows.Input.Key.Escape && down && !repeat) Close();
        else if (chooser != null && down && !repeat)
        {
            switch (key)
            {
                case System.Windows.Input.Key.Up: case System.Windows.Input.Key.Left:
                case System.Windows.Input.Key.Down: case System.Windows.Input.Key.Right:
                    Select(selected == PokemonPinballTitle.Original ? PokemonPinballTitle.RubySapphire : PokemonPinballTitle.Original); break;
                case System.Windows.Input.Key.Enter: case System.Windows.Input.Key.Space:
                    StartGame(selected); break;
                case System.Windows.Input.Key.D1: case System.Windows.Input.Key.NumPad1:
                    StartGame(PokemonPinballTitle.Original); break;
                case System.Windows.Input.Key.D2: case System.Windows.Input.Key.NumPad2:
                    StartGame(PokemonPinballTitle.RubySapphire); break;
            }
        }
        else if (Host != null)
        {
            if (down) heldKeys.Add(key);
            else heldKeys.Remove(key);
            Host.Forward(key, down, repeat);
        }
    }
    internal void ReleaseControls()
    {
        if (Host == null || disposed) return;
        foreach (Key key in heldKeys) Host.Forward(key, false, false);
        heldKeys.Clear();
    }
    private void Close() { if (disposed) return; Dispose(); closed(); }
    public void Dispose()
    {
        if (disposed) return; disposed = true; timer.Stop(); layer.SizeChanged -= Resize;
        setup.Cancel();
        setup.Dispose();
        if (Host != null) { Host.Stop(); Host.Dispose(); Host = null; }
        surface.Children.Remove(layer);
    }
}
