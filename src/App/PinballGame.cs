using System;
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
    private readonly StackPanel gameFrame;
    private readonly Action closed;
    private readonly TextBlock message;
    private readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
    private readonly CancellationTokenSource setup = new CancellationTokenSource();
    private readonly AppSettings settings;
    private string gameName;
    private Viewbox chooser;
    private Border controls;
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
        gameFrame = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        gameFrame.Children.Add(slot);
        layer.Children.Add(gameFrame); layer.Children.Add(message);
        surface.Children.Add(layer); layer.SizeChanged += Resize; Resize(null, null);
        timer.Tick += delegate
        {
            if (disposed || Host == null) return;
            if (Host.StartupError != null) { Fail("No se pudo iniciar el motor del juego."); return; }
            if (Host.Exited)
            { if (Host.ExitCode == 0) Close(); else Fail(gameName + " no pudo continuar. Puedes volver a intentarlo."); return; }
            Host.Poll();
            if (Host.ChildReady)
            {
                message.Visibility = Visibility.Collapsed;
                if (settings.Theme == AppTheme.Pokemon && controls == null)
                {
                    controls = PokemonPinballView.Controls();
                    gameFrame.Children.Add(controls);
                    Resize(null, null);
                }
            }
            else if (DateTime.UtcNow - nativeStarted > TimeSpan.FromSeconds(30)) Fail("El juego ha tardado demasiado en arrancar.");
        };
        timer.Start();
        if (settings.Theme == AppTheme.Pokemon) ShowChooser();
        else StartGame(PokemonPinballTitle.Original);
    }

    private void ShowChooser()
    {
        firstChoice = PokemonPinballView.Choice(PokemonPinballRom.Title(PokemonPinballTitle.Original), "Game Boy Color");
        secondChoice = PokemonPinballView.Choice(PokemonPinballRom.Title(PokemonPinballTitle.RubySapphire), "Game Boy Advance");
        var back = PokemonPinballView.Choice("Volver");
        firstChoice.Click += delegate { StartGame(PokemonPinballTitle.Original); };
        secondChoice.Click += delegate { StartGame(PokemonPinballTitle.RubySapphire); };
        back.Click += delegate { Close(); };
        chooser = PokemonPinballView.Chooser(firstChoice, secondChoice, back);
        Select(PokemonPinballTitle.Original);
        layer.Children.Add(chooser);
    }

    private void Select(PokemonPinballTitle game)
    {
        selected = game;
        PokemonPinballView.SetSelected(firstChoice, game == PokemonPinballTitle.Original);
        PokemonPinballView.SetSelected(secondChoice, game == PokemonPinballTitle.RubySapphire);
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
            if (string.IsNullOrWhiteSpace(rom))
            {
                string directory = System.IO.Path.Combine(PokemonPinballProcess.CacheRoot, "roms-europa");
                rom = await PokemonPinballRom.PrepareEuropeanAsync(game, directory, cancellation).ConfigureAwait(false);
                engine = PokemonPinballEngine.Mgba;
            }
            else engine = PokemonPinballRom.Validate(rom, game);
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
        double width = Math.Max(1, layer.ActualWidth - 36);
        double height = Math.Max(1, layer.ActualHeight - 36);
        if (controls != null)
        {
            controls.Width = Math.Min(700, width);
            controls.Measure(new Size(controls.Width, double.PositiveInfinity));
            height = Math.Max(1, height - controls.DesiredSize.Height);
        }
        double scale = Math.Max(0.1, Math.Min(1.5, Math.Min(width / gameWidth, height / gameHeight)));
        slot.Width = gameWidth * scale; slot.Height = gameHeight * scale;
    }
    private void Fail(string text)
    {
        if (Host != null) { Host.Dispose(); Host = null; slot.Children.Clear(); }
        if (controls != null)
        {
            gameFrame.Children.Remove(controls);
            controls = null;
        }
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
        else if (Host != null) Host.Forward(key, down, repeat);
    }
    internal void ReleaseControls()
    {
        if (Host != null && !disposed) Host.ReleaseControls();
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
