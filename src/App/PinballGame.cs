using System;
using System.IO;
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
    private bool disposed;
    private DateTime nativeStarted;
    internal PinballNativeHost Host { get; private set; }
    internal bool Loading { get; private set; }
    internal PinballGame(Grid root, Action onClosed)
    {
        surface = root; closed = onClosed; Loading = true;
        layer = new Grid { Background = new SolidColorBrush(Color.FromRgb(7, 12, 18)) };
        Panel.SetZIndex(layer, 200);
        slot = new Grid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        message = new TextBlock { Text = "Preparando el rincón del gato…", Foreground = Brushes.Bisque, FontSize = 18,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxWidth = 380 };
        layer.Children.Add(slot); layer.Children.Add(message);
        surface.Children.Add(layer); layer.SizeChanged += Resize; Resize(null, null);
        timer.Tick += delegate
        {
            if (disposed || Host == null) return;
            if (Host.StartupError != null) { Fail("No se pudo iniciar el motor del juego."); return; }
            if (Host.Exited)
            { if (Host.ExitCode == 0) Close(); else Fail("Cat Pinball no pudo continuar. Puedes volver a intentarlo."); return; }
            Host.Poll();
            if (Host.ChildReady) message.Visibility = Visibility.Collapsed;
            else if (DateTime.UtcNow - nativeStarted > TimeSpan.FromSeconds(30)) Fail("El juego ha tardado demasiado en arrancar.");
        };
        timer.Start();
        Task.Run(delegate { return PinballPayload.Prepare(); }).ContinueWith(delegate(Task<PinballPayload> task)
        {
            PinballPayload payload = null; Exception error = null;
            if (task.IsFaulted) error = task.Exception.GetBaseException();
            else payload = task.Result;
            surface.Dispatcher.BeginInvoke(new Action(delegate
            {
                if (disposed) return; Loading = false;
                if (error != null) { message.Text = "No se pudo preparar Cat Pinball.\n" + error.Message + "\nEsc para volver."; return; }
                try
                {
                    Host = new PinballNativeHost(payload);
                    nativeStarted = DateTime.UtcNow;
                    slot.Children.Add(Host);
                }
                catch (Exception ex) { message.Text = "No se pudo abrir Cat Pinball.\n" + ex.Message + "\nEsc para volver."; }
            }));
        });
    }
    private void Resize(object sender, SizeChangedEventArgs args)
    {
        double scale = Math.Max(0.1, Math.Min(1.5, Math.Min(Math.Max(1, layer.ActualWidth - 36) / 440, Math.Max(1, layer.ActualHeight - 36) / 860)));
        slot.Width = 440 * scale; slot.Height = 860 * scale;
    }
    private void Fail(string text)
    {
        if (Host != null) { Host.Dispose(); Host = null; slot.Children.Clear(); }
        message.Text = text + "\nEsc para volver."; message.Visibility = Visibility.Visible;
    }
    internal void Key(Key key, bool down, bool repeat)
    {
        if (disposed) return;
        if (key == System.Windows.Input.Key.Escape && down) Close();
        else if (Host != null) Host.Forward(key, down, repeat);
    }
    internal void ReleaseControls()
    {
        if (Host == null || disposed) return;
        Host.Forward(System.Windows.Input.Key.LeftCtrl, false, false);
        Host.Forward(System.Windows.Input.Key.RightCtrl, false, false);
    }
    private void Close() { if (disposed) return; Dispose(); closed(); }
    public void Dispose()
    {
        if (disposed) return; disposed = true; timer.Stop(); layer.SizeChanged -= Resize;
        if (Host != null) { Host.Stop(); Host.Dispose(); Host = null; }
        surface.Children.Remove(layer);
    }
}
