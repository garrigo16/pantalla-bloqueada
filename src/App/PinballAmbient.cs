using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

// Autor: Miquel Garrigo
internal sealed class PinballAmbient : IDisposable
{
    private static readonly BitmapSource Background = Load("PinballNook.png");
    private static readonly BitmapSource ClosedEyes = Load("PinballBlink.png");
    private readonly Random random = new Random();
    private readonly List<TranslateTransform> rain = new List<TranslateTransform>();
    private readonly DispatcherTimer blinkTimer = new DispatcherTimer();
    private readonly Image blinking;
    private readonly Ellipse lamp;
    private bool disposed;
    internal double RainOffset { get { return rain.Count == 0 ? 0 : rain[0].Y; } }
    internal double BlinkOpacity { get { return blinking.Opacity; } }
    internal PinballAmbient(Canvas table)
    {
        table.Children.Add(new Image { Source = Background, Width = 440, Height = 660, IsHitTestVisible = false });
        var glass = new GeometryGroup();
        glass.Children.Add(new RectangleGeometry(new Rect(141, 24, 43, 22)));
        glass.Children.Add(new RectangleGeometry(new Rect(141, 57, 48, 54)));
        glass.Children.Add(Geometry.Parse("M108,22 L130,19 L130,46 L106,46 Z"));
        var outside = new Canvas { Width = 440, Height = 660, Clip = glass, IsHitTestVisible = false };
        table.Children.Add(outside);
        for (int i = 0; i < 28; i++)
        {
            double x = 100 + random.NextDouble() * 108;
            var drop = new Line { X1 = x, X2 = x - 4, Y1 = -8, Y2 = 1, StrokeThickness = 0.7,
                Stroke = new SolidColorBrush(Color.FromArgb(110, 198, 222, 237)), Opacity = 0.35 + random.NextDouble() * 0.35 };
            var motion = new TranslateTransform(); drop.RenderTransform = motion; rain.Add(motion); outside.Children.Add(drop);
            motion.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-12, 130, TimeSpan.FromSeconds(1.1 + random.NextDouble() * 0.9))
                { BeginTime = TimeSpan.FromSeconds(-random.NextDouble() * 2), RepeatBehavior = RepeatBehavior.Forever });
        }
        lamp = new Ellipse { Width = 68, Height = 50, IsHitTestVisible = false, Opacity = 0.1,
            Fill = new RadialGradientBrush(Color.FromArgb(180, 255, 205, 102), Colors.Transparent) };
        Canvas.SetLeft(lamp, 205); Canvas.SetTop(lamp, 20); table.Children.Add(lamp);
        lamp.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.07, 0.19, TimeSpan.FromSeconds(2.8)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
        var eyes = new GeometryGroup();
        eyes.Children.Add(new EllipseGeometry(new Point(337, 75), 13, 16));
        eyes.Children.Add(new EllipseGeometry(new Point(364, 68), 14, 16));
        blinking = new Image { Source = ClosedEyes, Width = 440, Height = 660, Clip = eyes, Opacity = 0, IsHitTestVisible = false };
        table.Children.Add(blinking);
        blinkTimer.Interval = TimeSpan.FromSeconds(3 + random.NextDouble() * 3);
        blinkTimer.Tick += delegate { Blink(); blinkTimer.Interval = TimeSpan.FromSeconds(4 + random.NextDouble() * 5); };
        blinkTimer.Start();
    }
    private static BitmapSource Load(string name)
    {
        using (var input = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
        {
            if (input == null) throw new InvalidOperationException("Falta una imagen del pinball.");
            var image = BitmapFrame.Create(input, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            image.Freeze(); return image;
        }
    }
    internal void Blink()
    {
        if (disposed) return;
        var frames = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop };
        frames.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        frames.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(70))));
        frames.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(130))));
        frames.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(220))));
        if (random.Next(4) == 0)
        {
            frames.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300))));
            frames.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(370))));
            frames.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(420))));
            frames.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(510))));
        }
        blinking.BeginAnimation(UIElement.OpacityProperty, frames);
    }
    public void Dispose()
    {
        if (disposed) return; disposed = true; blinkTimer.Stop();
        foreach (var motion in rain) motion.BeginAnimation(TranslateTransform.YProperty, null);
        blinking.BeginAnimation(UIElement.OpacityProperty, null); lamp.BeginAnimation(UIElement.OpacityProperty, null);
    }
}
