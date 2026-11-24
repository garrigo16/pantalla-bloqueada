using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

// Autor: Miquel Garrigo
internal sealed class PinballGame : IDisposable
{
    private readonly Grid surface, layer;
    private readonly Canvas field;
    private readonly Action closed;
    private readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000.0 / 60) };
    private readonly Stopwatch elapsed = Stopwatch.StartNew();
    private double previous, lastInput, overAt = -1;
    private bool disposed;
    internal readonly PinballPhysics Physics = new PinballPhysics();
    internal readonly PinballAmbient Ambient;
    private readonly TextBlock score, lives, status;
    private readonly Canvas yarn;
    private readonly Line leftOutline, leftPaw, rightOutline, rightPaw;
    private readonly Ellipse[] leftBeans = new Ellipse[3], rightBeans = new Ellipse[3];
    private readonly Ellipse[] bumpers = new Ellipse[5];
    private static readonly Brush Cream = new SolidColorBrush(Color.FromRgb(248, 225, 182));
    private static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(43, 41, 37));
    internal PinballGame(Grid root, Action onClosed)
    {
        surface = root; closed = onClosed;
        layer = new Grid { Background = new SolidColorBrush(Color.FromArgb(246, 7, 12, 18)), IsHitTestVisible = false };
        Panel.SetZIndex(layer, 200);
        var table = new Canvas { Width = 440, Height = 720, Background = new SolidColorBrush(Color.FromRgb(14, 23, 26)) };
        Ambient = new PinballAmbient(table);
        var view = new Viewbox { Child = table, Stretch = Stretch.Uniform, Margin = new Thickness(18),
            MaxWidth = Math.Max(220, Math.Min(660, surface.ActualWidth - 36)), MaxHeight = Math.Max(300, surface.ActualHeight - 36),
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        layer.Children.Add(view);
        var title = Label("El rincón del gato", 20); title.FontWeight = FontWeights.SemiBold;
        Put(table, title, 60, 131); title.Width = 320; title.TextAlignment = TextAlignment.Center;
        score = Label("Ronroneos  000000", 13); lives = Label("Ovillos  3", 13);
        Put(table, score, 67, 157); Put(table, lives, 310, 157);
        field = new Canvas { Width = PinballPhysics.Width, Height = PinballPhysics.Height };
        Put(table, field, 60, 178);
        var boundary = new Path { Stroke = new SolidColorBrush(Color.FromArgb(145, 229, 191, 120)), StrokeThickness = 2,
            Data = Geometry.Parse("M12,430 L12,28 Q12,12 28,12 L292,12 Q308,12 308,28 L308,430") };
        field.Children.Add(boundary);
        AddBumper(0, 102, 103, 19, "✦"); AddBumper(1, 218, 103, 19, "✦"); AddBumper(2, 160, 190, 22, "☾");
        AddBumper(3, 65, 308, 17, "❀"); AddBumper(4, 255, 308, 17, "❀");
        AddLine(12, 275, 79, 357, 4, Cream); AddLine(308, 275, 241, 357, 4, Cream);
        leftOutline = AddLine(82, 376, 144, 402, 20, Cream); rightOutline = AddLine(238, 376, 176, 402, 20, Cream);
        leftPaw = AddLine(82, 376, 144, 402, 15, Ink); rightPaw = AddLine(238, 376, 176, 402, 15, Ink);
        for (int i = 0; i < 3; i++)
        {
            leftBeans[i] = Bean(); rightBeans[i] = Bean();
            field.Children.Add(leftBeans[i]); field.Children.Add(rightBeans[i]);
        }
        yarn = new Canvas { Width = 14, Height = 14 };
        yarn.Children.Add(new Ellipse { Width = 14, Height = 14, Fill = new SolidColorBrush(Color.FromRgb(246, 207, 145)), Stroke = Cream, StrokeThickness = 1 });
        yarn.Children.Add(new Path { Stroke = new SolidColorBrush(Color.FromRgb(159, 104, 67)), StrokeThickness = 1,
            Data = Geometry.Parse("M2,4 Q8,1 12,8 M2,8 Q5,4 10,3 M4,12 Q5,7 11,5") });
        field.Children.Add(yarn);
        status = Label("Espacio para lanzar el ovillo", 15); status.TextAlignment = TextAlignment.Center; status.Width = 300;
        Put(field, status, 10, 239);
        var labels = Label("Ctrl izquierdo                         Ctrl derecho", 12); Put(table, labels, 90, 612);
        var help = Label("Espacio · Lanzar     P · Pausa     Esc · Volver", 13); help.Width = 440; help.TextAlignment = TextAlignment.Center; Put(table, help, 0, 669);
        var quiet = Label("Una partida entre libros y lluvia.", 12); quiet.Opacity = 0.6; quiet.Width = 440; quiet.TextAlignment = TextAlignment.Center; Put(table, quiet, 0, 692);
        surface.Children.Add(layer); timer.Tick += Tick; timer.Start(); Render();
    }
    private static TextBlock Label(string text, double size)
    { return new TextBlock { Text = text, FontSize = size, Foreground = Cream, FontFamily = new FontFamily("Segoe UI"), TextWrapping = TextWrapping.Wrap }; }
    private static void Put(Canvas canvas, UIElement item, double x, double y) { Canvas.SetLeft(item, x); Canvas.SetTop(item, y); canvas.Children.Add(item); }
    private static Ellipse Bean() { return new Ellipse { Width = 4, Height = 4, Fill = new SolidColorBrush(Color.FromRgb(220, 151, 138)) }; }
    private Line AddLine(double ax, double ay, double bx, double by, double width, Brush color)
    {
        var line = new Line { X1 = ax, Y1 = ay, X2 = bx, Y2 = by, Stroke = color, StrokeThickness = width, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
        field.Children.Add(line); return line;
    }
    private void AddBumper(int index, double x, double y, double radius, string symbol)
    {
        var ring = new Ellipse { Width = radius * 2, Height = radius * 2, Stroke = Cream, StrokeThickness = 2,
            Fill = new SolidColorBrush(Color.FromRgb(152, 96, 70)) };
        Put(field, ring, x - radius, y - radius); bumpers[index] = ring;
        var label = Label(symbol, 23); label.Width = radius * 2; label.TextAlignment = TextAlignment.Center;
        Put(field, label, x - radius, y - 17);
    }
    private void Tick(object sender, EventArgs args)
    {
        if (disposed) return;
        double now = elapsed.Elapsed.TotalSeconds;
        Physics.Step(now - previous); previous = now;
        if (Physics.GameOver && overAt < 0) overAt = now;
        if (!Physics.GameOver) overAt = -1;
        if (now - lastInput > 60 || (overAt >= 0 && now - overAt > 12)) { Close(); return; }
        Render();
    }
    private void Render()
    {
        score.Text = "Ronroneos  " + Physics.Score.ToString("D6"); lives.Text = "Ovillos  " + Physics.Lives;
        status.Text = Physics.Paused ? "Pausa entre páginas\nP para continuar" :
            Physics.GameOver ? "Fin de la partida\n" + Physics.Score + " ronroneos\nEspacio para jugar otra vez" : Physics.Ready ? "Espacio para lanzar el ovillo" : "";
        yarn.Visibility = Physics.GameOver ? Visibility.Collapsed : Visibility.Visible;
        Canvas.SetLeft(yarn, Physics.X - 7); Canvas.SetTop(yarn, Physics.Y - 7);
        SetPaw(leftOutline, leftPaw, leftBeans, Physics.Left ? 142 : 144, Physics.Left ? 345 : 402);
        SetPaw(rightOutline, rightPaw, rightBeans, Physics.Right ? 178 : 176, Physics.Right ? 345 : 402);
        for (int i = 0; i < bumpers.Length; i++) bumpers[i].Opacity = Physics.LastBumper == i && Physics.BumperFlash > 0 ? 1 : 0.73;
    }
    private static void SetPaw(Line outline, Line paw, Ellipse[] beans, double x, double y)
    {
        outline.X2 = paw.X2 = x; outline.Y2 = paw.Y2 = y;
        double angle = Math.Atan2(y - paw.Y1, x - paw.X1);
        for (int i = 0; i < 3; i++)
        {
            Canvas.SetLeft(beans[i], x - Math.Cos(angle) * 5 - Math.Sin(angle) * (i - 1) * 3.8 - 2);
            Canvas.SetTop(beans[i], y - Math.Sin(angle) * 5 + Math.Cos(angle) * (i - 1) * 3.8 - 2);
        }
    }
    internal void Key(Key key, bool down, bool repeat)
    {
        if (disposed) return; lastInput = elapsed.Elapsed.TotalSeconds;
        if (key == System.Windows.Input.Key.LeftCtrl) Physics.Left = down;
        else if (key == System.Windows.Input.Key.RightCtrl) Physics.Right = down;
        else if (down && !repeat)
        {
            if (key == System.Windows.Input.Key.Space) Physics.Launch();
            else if (key == System.Windows.Input.Key.P) Physics.Paused = !Physics.Paused;
            else if (key == System.Windows.Input.Key.Escape) Close();
        }
        if (!disposed) Render();
    }
    internal void ReleaseControls() { Physics.ReleaseControls(); }
    private void Close() { if (disposed) return; Dispose(); closed(); }
    public void Dispose()
    {
        if (disposed) return; disposed = true; timer.Stop(); elapsed.Stop(); Physics.ReleaseControls(); Ambient.Dispose(); surface.Children.Remove(layer);
    }
}
