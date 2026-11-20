using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

// Autor: Miquel Garrigo
internal sealed class FunOverlay : IDisposable
{
    private readonly Grid surface;
    private readonly FrameworkElement clock;
    private readonly DispatcherTimer timer = new DispatcherTimer();
    private Grid layer;
    private bool disposed;
    internal FunOverlay(Grid root, FrameworkElement time)
    {
        surface = root; clock = time;
        timer.Tick += delegate
        {
            timer.Stop();
            var fading = layer;
            if (fading == null) return;
            var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(180));
            fade.Completed += delegate { if (layer == fading) Clear(); };
            fading.BeginAnimation(UIElement.OpacityProperty, fade);
        };
    }

    internal void Show(FunReaction reaction, Point? pointer)
    {
        if (disposed || reaction == null) return;
        Clear();
        layer = new Grid { IsHitTestVisible = false };
        Panel.SetZIndex(layer, 100);
        surface.Children.Add(layer);
        FrameworkElement content;
        if (reaction.Kind == FunKind.Eyes) content = Eyes();
        else if (reaction.Kind == FunKind.Konami) content = Mascot(reaction.Text);
        else
        {
            var body = new StackPanel();
            if (reaction.Kind == FunKind.Sleepy) body.Children.Add(Label("Z z z", 34, Brushes.LightSkyBlue));
            if (reaction.Kind == FunKind.Magic) body.Children.Add(Label("✦  ✧  ✦", 38, Brushes.Gold));
            if (reaction.Kind == FunKind.Embarrassed) body.Children.Add(Curtain());
            body.Children.Add(Label(reaction.Text, reaction.Kind == FunKind.Networks ? 18 : 23,
                reaction.Kind == FunKind.PostIt ? Brushes.MidnightBlue : (reaction.Kind == FunKind.Stamp ? Brushes.MediumSpringGreen : Brushes.White)));
            var card = new Border { Child = body, Padding = new Thickness(23, 17, 23, 17), CornerRadius = new CornerRadius(10),
                MaxWidth = Math.Max(240, Math.Min(600, surface.ActualWidth - 50)),
                Background = reaction.Kind == FunKind.PostIt ? new SolidColorBrush(Color.FromRgb(255, 220, 88)) : new SolidColorBrush(Color.FromArgb(205, 15, 23, 40)) };
            if (reaction.Kind == FunKind.Stamp) { card.BorderBrush = Brushes.MediumSpringGreen; card.BorderThickness = new Thickness(3); }
            if (reaction.Kind == FunKind.PostIt || reaction.Kind == FunKind.Stamp)
                card.RenderTransform = new RotateTransform(reaction.Kind == FunKind.PostIt ? 3 : -4);
            content = card;
        }
        content.HorizontalAlignment = HorizontalAlignment.Center;
        content.VerticalAlignment = VerticalAlignment.Center;
        if (reaction.Kind == FunKind.Pixel)
        { content.HorizontalAlignment = HorizontalAlignment.Left; content.VerticalAlignment = VerticalAlignment.Bottom; content.Margin = new Thickness(32); }
        if (reaction.Kind == FunKind.Eyes && pointer.HasValue && surface.ActualWidth > 120 && surface.ActualHeight > 80)
        {
            var canvas = new Canvas();
            Canvas.SetLeft(content, Math.Max(15, Math.Min(surface.ActualWidth - 115, pointer.Value.X - 50)));
            Canvas.SetTop(content, Math.Max(15, Math.Min(surface.ActualHeight - 60, pointer.Value.Y - 25)));
            canvas.Children.Add(content); layer.Children.Add(canvas);
        }
        else layer.Children.Add(content);
        if (reaction.Kind == FunKind.Shake) content.Visibility = Visibility.Collapsed;
        if (reaction.Kind == FunKind.Konami)
        {
            var travel = new TranslateTransform(); content.RenderTransform = travel;
            travel.BeginAnimation(TranslateTransform.XProperty,
                new DoubleAnimation(-surface.ActualWidth / 2 - 180, surface.ActualWidth / 2 + 180, TimeSpan.FromMilliseconds(1900)));
        }
        AnimateClock(reaction.Kind);
        layer.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(120)));
        timer.Interval = TimeSpan.FromMilliseconds(reaction.Milliseconds); timer.Start();
    }

    private static TextBlock Label(string text, double size, Brush color)
    { return new TextBlock { Text = text, FontFamily = new FontFamily("Segoe UI"), FontSize = size, Foreground = color,
        TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxWidth = 520, Margin = new Thickness(2) }; }

    private static FrameworkElement Eyes()
    {
        var eyes = new StackPanel { Orientation = Orientation.Horizontal, Width = 104, Height = 48, Opacity = 0.75 };
        for (int i = 0; i < 2; i++)
        {
            var eye = new Grid { Width = 44, Height = 36, Margin = new Thickness(4) };
            eye.Children.Add(new Ellipse { Fill = Brushes.White });
            eye.Children.Add(new Ellipse { Width = 13, Height = 22, Fill = Brushes.Black, Margin = new Thickness(5, 2, 0, 0) });
            eyes.Children.Add(eye);
        }
        return eyes;
    }

    private static FrameworkElement Curtain()
    {
        var cloth = new Grid { Height = 95, Margin = new Thickness(0, 0, 0, 12), ClipToBounds = true };
        for (int i = 0; i < 12; i++)
        {
            cloth.ColumnDefinitions.Add(new ColumnDefinition());
            var fold = new Border { CornerRadius = new CornerRadius(0, 0, 12, 12),
                Background = new LinearGradientBrush(Color.FromRgb(104, 20, 40), Color.FromRgb(202, 58, 77), 0) };
            Grid.SetColumn(fold, i); cloth.Children.Add(fold);
        }
        var drop = new ScaleTransform(1, 1); cloth.RenderTransform = drop;
        drop.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300)));
        return cloth;
    }

    private static FrameworkElement Mascot(string text)
    {
        var panel = new StackPanel { Width = 330 };
        var drawing = new Canvas { Width = 112, Height = 112, HorizontalAlignment = HorizontalAlignment.Center };
        drawing.Children.Add(new Path { Data = Geometry.Parse("M30,48 L30,30 C30,4 82,4 82,30 L82,48"), Stroke = Brushes.LightSkyBlue, StrokeThickness = 8 });
        var body = new Rectangle { Width = 86, Height = 65, RadiusX = 13, RadiusY = 13, Fill = Brushes.DeepSkyBlue };
        Canvas.SetLeft(body, 13); Canvas.SetTop(body, 42); drawing.Children.Add(body);
        for (int i = 0; i < 2; i++)
        {
            var glasses = new Rectangle { Width = 25, Height = 16, RadiusX = 4, RadiusY = 4, Fill = Brushes.Black };
            Canvas.SetLeft(glasses, 25 + i * 36); Canvas.SetTop(glasses, 60); drawing.Children.Add(glasses);
        }
        var bridge = new Rectangle { Width = 16, Height = 4, Fill = Brushes.Black };
        Canvas.SetLeft(bridge, 48); Canvas.SetTop(bridge, 63); drawing.Children.Add(bridge);
        drawing.Children.Add(new Path { Data = Geometry.Parse("M39,85 Q56,100 73,85"), Stroke = Brushes.Black, StrokeThickness = 3 });
        panel.Children.Add(drawing); panel.Children.Add(Label(text, 18, Brushes.White));
        return panel;
    }

    private void AnimateClock(FunKind kind)
    {
        if (clock == null || !clock.IsVisible) return;
        clock.RenderTransformOrigin = new Point(0.5, 0.5);
        if (kind == FunKind.Sleepy)
        {
            var scale = new ScaleTransform(1, 1); clock.RenderTransform = scale;
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1, 0.87, TimeSpan.FromMilliseconds(350)) { AutoReverse = true, RepeatBehavior = new RepeatBehavior(2) });
        }
        else if (kind == FunKind.Shake || kind == FunKind.Houdini || kind == FunKind.Evade)
        {
            var move = new TranslateTransform(); clock.RenderTransform = move;
            move.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, kind == FunKind.Evade ? 85 : 12,
                TimeSpan.FromMilliseconds(kind == FunKind.Evade ? 230 : 75)) { AutoReverse = true, RepeatBehavior = new RepeatBehavior(kind == FunKind.Evade ? 1 : 3) });
        }
    }

    internal void Clear()
    {
        timer.Stop();
        if (layer != null) { layer.BeginAnimation(UIElement.OpacityProperty, null); surface.Children.Remove(layer); layer = null; }
        if (clock != null) clock.RenderTransform = Transform.Identity;
    }
    public void Dispose() { disposed = true; Clear(); }
}
