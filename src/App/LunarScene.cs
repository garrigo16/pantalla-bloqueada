using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

// Autor: Miquel Garrigo
internal sealed class LunarScene : FrameworkElement, IDisposable
{
    private sealed class Actor
    {
        internal LunarClip Clip;
        internal double Meters, Size = 1, Offset;
        internal int StandingPixels;
        internal Point Position;
        internal bool Flip;
        internal Dictionary<LunarFrame, BitmapSource> StillImages;
    }

    private sealed class Leg
    {
        internal Point From, To;
        internal double Duration;
        internal int Direction;
        internal bool Moving;
    }

    private sealed class Visit
    {
        internal Actor Actor;
        internal Leg[] Legs;
        internal double Start, End;
    }

    private sealed class Stop
    {
        internal readonly Rect Area;
        internal readonly Point LookAt;
        internal Stop(Rect area, Point lookAt) { Area = area; LookAt = lookAt; }
    }

    private const double PixelsPerMeter = 54;
    private static readonly Rect View = new Rect(0, 0, LunarBackdrop.Width, LunarBackdrop.Height);
    private static readonly Brush Shadow = new SolidColorBrush(Color.FromArgb(102, 7, 20, 35));
    private static readonly Stop[] Stops = {
        new Stop(new Rect(221, 204, 36, 11), new Point(242, 174)),
        new Stop(new Rect(192, 217, 31, 12), new Point(120, 194)),
        new Stop(new Rect(278, 229, 33, 12), new Point(268, 194))
    };
    private static readonly int[,] Glances = {
        { 7, 120 }, { 6, 790 }, { 7, 130 }, { 0, 180 }, { 1, 120 }, { 2, 790 }, { 1, 130 }, { 0, 240 }
    };
    private static readonly double GlanceDuration = Enumerable.Range(0, Glances.GetLength(0)).Sum(i => Glances[i, 1]);

    private readonly LunarBackdrop art = LunarBackdrop.Resource.Value;
    private readonly LunarClip wake = new LunarClip("0025", "Wake", 40, 40, 8, 6, 4, 14, 10);
    private readonly LunarClip look = new LunarClip("0025", "Walk", 32, 40, 8, 10, 8, 10);
    private readonly Actor[] sleepers, guests;
    private readonly List<Actor> ready = new List<Actor>();
    private readonly DispatcherTimer tick = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(40) };
    private readonly CancellationTokenSource cancel = new CancellationTokenSource();
    private readonly Stopwatch time = new Stopwatch();
    private readonly Random random = new Random();
    private Visit visitor;
    private Actor previousGuest;
    private int previousStop = -1;
    private int untilWake;
    private double clock, lastTick, nextVisit = 1550, wakingAt = double.PositiveInfinity;
    private bool running, disposed;

    static LunarScene() { Shadow.Freeze(); }

    internal LunarScene()
    {
        IsHitTestVisible = false;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
        sleepers = new[] {
            Sleeper("0143", 40, 24, 2.1, 27, 105, 193, true, .85, 0),
            Sleeper("0025", 32, 40, .4, 25, 124, 200, false, 1, 0),
            Sleeper("0133", 32, 24, .3, 18, 142, 200, true, 1, 550)
        };
        guests = new[] {
            Guest("0001", 40, 40, .7, 21, 4, 4, 4, 4, 4, 4),
            Guest("0004", 32, 32, .6, 22, 6, 8, 6, 8),
            Guest("0007", 32, 32, .5, 18, 12, 8, 12, 8),
            Guest("0037", 32, 40, .6, 20, 6, 4, 4, 4, 6),
            Guest("0043", 24, 40, .5, 20, 8, 4, 8, 6, 8, 4, 8, 6),
            Guest("0054", 24, 40, .8, 24, 8, 12, 8, 12)
        };
        untilWake = random.Next(2, 6);
        tick.Tick += Tick;
        foreach (var actor in sleepers) Prepare(actor.Clip);
        Prepare(wake);
        Prepare(look);
        foreach (var actor in guests) Prepare(actor.Clip, actor);
    }

    private static Actor Sleeper(string id, int width, int height, double meters, int standingPixels,
        double x, double y, bool flip, double size, double offset)
    {
        return new Actor { Clip = new LunarClip(id, "Sleep", width, height, 30, 35), Meters = meters,
            StandingPixels = standingPixels, Position = new Point(x, y), Flip = flip, Size = size, Offset = offset,
            StillImages = new Dictionary<LunarFrame, BitmapSource>() };
    }

    private static Actor Guest(string id, int width, int height, double meters, int standingPixels, params int[] ticks)
    {
        return new Actor { Clip = new LunarClip(id, "Walk", width, height, ticks), Meters = meters, StandingPixels = standingPixels };
    }

    private async void Prepare(LunarClip clip, Actor guest = null)
    {
        var token = cancel.Token;
        LunarFrame[][] frames;
        try { frames = await Task.Run(delegate { return LunarAssets.Load(clip, token); }).ConfigureAwait(false); }
        catch (Exception ex) { Trace.TraceWarning("Error al preparar la orilla: " + ex.GetType().Name); return; }
        if (frames == null || token.IsCancellationRequested || Dispatcher.HasShutdownStarted) return;
        try
        {
            await Dispatcher.InvokeAsync(delegate
            {
                if (disposed) return;
                clip.Frames = frames;
                if (guest != null) ready.Add(guest);
                InvalidateVisual();
            }).Task.ConfigureAwait(false);
        }
        catch (TaskCanceledException) { }
    }

    internal void SetRunning(bool value)
    {
        if (disposed || running == value) return;
        running = value;
        if (running)
        {
            time.Restart();
            lastTick = 0;
            tick.Start();
        }
        else { tick.Stop(); time.Stop(); }
    }

    private void Tick(object sender, EventArgs args)
    {
        double now = time.Elapsed.TotalMilliseconds;
        clock += Math.Min(120, now - lastTick);
        lastTick = now;
        double wakeDuration = 2 * wake.Duration + GlanceDuration;
        if (clock >= wakingAt + wakeDuration) wakingAt = double.PositiveInfinity;
        if (visitor != null && clock >= visitor.End)
        {
            visitor = null;
            nextVisit = clock + Number(1700, 4100);
            if (--untilWake == 0)
            {
                untilWake = random.Next(2, 6);
                if (wake.Frames != null && look.Frames != null)
                {
                    wakingAt = clock + Number(400, 850);
                    nextVisit += wakingAt - clock + wakeDuration;
                }
            }
        }
        if (visitor == null && double.IsPositiveInfinity(wakingAt) && clock >= nextVisit && ready.Count != 0)
            BeginVisit();
        InvalidateVisual();
    }

    private double Number(double min, double max) { return min + (max - min) * random.NextDouble(); }

    private int OtherIndex(int count, int previous)
    {
        if (previous < 0 || count < 2) return random.Next(count);
        int index = random.Next(count - 1);
        return index >= previous ? index + 1 : index;
    }

    private Point Entrance(int index)
    {
        if (index == 0) return new Point(-46, Number(250, 258));
        if (index == 1) return new Point(View.Width + 46, Number(229, 249));
        return new Point(Number(220, 383), View.Height + 58);
    }

    private static int Direction(Point from, Point to)
    {
        return ((int)Math.Floor(Math.Atan2(to.X - from.X, to.Y - from.Y) / (Math.PI / 4) + .5) + 8) % 8;
    }

    private static Leg Walk(Point from, Point to, double speed)
    {
        return new Leg { From = from, To = to, Direction = Direction(from, to),
            Moving = true, Duration = 1000 * (to - from).Length / speed };
    }

    private void BeginVisit()
    {
        previousGuest = ready[OtherIndex(ready.Count, ready.IndexOf(previousGuest))];
        previousStop = OtherIndex(Stops.Length, previousStop);
        int entry = random.Next(3), exit = OtherIndex(3, entry);
        var stop = Stops[previousStop];
        var center = new Point(Number(237, 286), Number(230, 243));
        var destination = new Point(Number(stop.Area.Left, stop.Area.Right), Number(stop.Area.Top, stop.Area.Bottom));
        double speed = Number(41, 51);
        var legs = new[] { Walk(Entrance(entry), center, speed), Walk(center, destination, speed),
            new Leg { From = destination, To = destination, Direction = Direction(destination, stop.LookAt), Duration = Number(1950, 3800) },
            Walk(destination, Entrance(exit), speed) };
        visitor = new Visit { Actor = previousGuest, Legs = legs, Start = clock, End = clock + legs.Sum(leg => leg.Duration) };
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        double scale = Math.Min(ActualWidth / View.Width, ActualHeight / View.Height);
        dc.PushTransform(new MatrixTransform(scale, 0, 0, scale, (ActualWidth - View.Width * scale) / 2, (ActualHeight - View.Height * scale) / 2));
        dc.PushClip(new RectangleGeometry(View));
        dc.DrawImage(art.Image, View);
        for (int i = 0; i < sleepers.Length; i++)
        {
            var actor = sleepers[i];
            var pose = i == 1 && clock >= wakingAt ? PikachuAwake() : actor.Clip.At(0, (clock + actor.Offset) % actor.Clip.Duration);
            Draw(dc, actor, pose, actor.Position);
        }
        if (visitor != null)
        {
            dc.PushClip(art.VisitorClip);
            DrawVisitor(dc);
            dc.Pop();
        }
        dc.Pop();
        dc.Pop();
    }

    private LunarFrame PikachuAwake()
    {
        double elapsed = clock - wakingAt;
        if (elapsed < wake.Duration) return wake.At(0, elapsed);
        double looking = elapsed - wake.Duration;
        for (int i = 0; i < Glances.GetLength(0); i++)
        {
            if (looking < Glances[i, 1]) return look.At(Glances[i, 0], 0);
            looking -= Glances[i, 1];
        }
        return wake.At(0, wake.Duration - looking);
    }

    private void DrawVisitor(DrawingContext dc)
    {
        double elapsed = clock - visitor.Start;
        foreach (var leg in visitor.Legs)
        {
            if (elapsed >= leg.Duration) { elapsed -= leg.Duration; continue; }
            var clip = visitor.Actor.Clip;
            var pose = clip.At(leg.Direction, leg.Moving ? (clock - visitor.Start) % clip.Duration : 0);
            Draw(dc, visitor.Actor, pose, leg.From + (leg.To - leg.From) * (elapsed / leg.Duration));
            break;
        }
    }

    private static void Draw(DrawingContext dc, Actor actor, LunarFrame pose, Point point)
    {
        if (pose == null) return;
        double scale = PixelsPerMeter * actor.Meters / actor.StandingPixels * actor.Size;
        double x = Math.Round(point.X), y = Math.Round(point.Y);
        double radius = Math.Round(pose.InkWidth * scale * .41);
        dc.DrawEllipse(Shadow, null, new Point(x, y - 2), radius, Math.Round(Math.Max(2, radius * .07)));
        BitmapSource bitmap;
        if (actor.StillImages == null || !actor.StillImages.TryGetValue(pose, out bitmap))
        {
            bitmap = ApplyMoonlight(pose, point, actor.Flip);
            if (actor.StillImages != null) actor.StillImages.Add(pose, bitmap);
        }
        if (actor.Flip) dc.PushTransform(new ScaleTransform(-1, 1, x, y));
        dc.DrawImage(bitmap, new Rect(x + Math.Round(-pose.Center * scale), y + Math.Round(-pose.Bottom * scale),
            Math.Round(pose.Width * scale), Math.Round(pose.Height * scale)));
        if (actor.Flip) dc.Pop();
    }

    private static BitmapSource ApplyMoonlight(LunarFrame pose, Point point, bool flip)
    {
        double dx = 260 - point.X, dy = 63 - point.Y;
        double fromReflection = dx / 103, fromWater = (point.Y - 193) / 64;
        double light = Math.Exp(-(fromReflection * fromReflection + fromWater * fromWater));
        double fromSide = (flip ? -1 : 1) * dx / Math.Sqrt(dx * dx + dy * dy);
        var pixels = (byte[])pose.Pixels.Clone();
        for (int i = 0; i < pixels.Length; i += 4)
        {
            if (pixels[i + 3] == 0) continue;
            int column = i / 4 % pose.Width, row = i / 4 / pose.Width;
            double highlight = light * (.65 + .35 * (1 - (double)row / pose.Bottom) + .35 * fromSide * (column - pose.Center) / pose.Width);
            double red = pixels[i + 2], green = pixels[i + 1], blue = pixels[i];
            double luminance = .213 * red + .715 * green + .072 * blue;
            pixels[i + 2] = Channel(red * (.50 + .29 * highlight) + luminance * .10 * highlight + 5);
            pixels[i + 1] = Channel(green * (.56 + .27 * highlight) + luminance * (.05 + .10 * highlight) + 9);
            pixels[i] = Channel(blue * (.68 + .23 * highlight) + luminance * (.16 + .19 * highlight) + 15);
        }
        var bitmap = BitmapSource.Create(pose.Width, pose.Height, 96, 96, PixelFormats.Bgra32, null, pixels, pose.Width * 4);
        bitmap.Freeze();
        return bitmap;
    }

    private static byte Channel(double value) { return (byte)Math.Max(0, Math.Min(255, Math.Round(value))); }

    public void Dispose()
    {
        if (disposed) return;
        SetRunning(false);
        disposed = true;
        tick.Tick -= Tick;
        cancel.Cancel();
        cancel.Dispose();
    }
}

internal sealed partial class LockWindow
{
    private LunarScene lunarScene;
    private DispatcherTimer lunarDelayTimer;

    private void PrepareLunarScene()
    {
        lunarScene = new LunarScene { Visibility = Visibility.Collapsed, Opacity = 0 };
        Panel.SetZIndex(lunarScene, 10);
        Surface.Children.Add(lunarScene);
    }

    private void ScheduleLunarScene()
    {
        if (lunarScene == null || lunarDelayTimer != null || lunarScene.Visibility == Visibility.Visible) return;
        lunarDelayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Math.Max(1, preferences.LunarDelaySeconds * 1000.0)) };
        lunarDelayTimer.Tick += ShowLunarScene;
        lunarDelayTimer.Start();
    }

    private void ShowLunarScene(object sender, EventArgs args)
    {
        lunarDelayTimer.Stop();
        lunarDelayTimer.Tick -= ShowLunarScene;
        lunarDelayTimer = null;
        peekTimer.Stop();
        imageLayer.Visibility = Visibility.Collapsed;
        if (timer != null) timer.Stop();
        lunarScene.Visibility = Visibility.Visible;
        lunarScene.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(2400))
        {
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        });
        lunarScene.SetRunning(!HasPinball);
    }

    private void StopLunarScene()
    {
        if (lunarDelayTimer != null)
        {
            lunarDelayTimer.Stop();
            lunarDelayTimer.Tick -= ShowLunarScene;
            lunarDelayTimer = null;
        }
        if (lunarScene == null) return;
        lunarScene.BeginAnimation(OpacityProperty, null);
        lunarScene.Dispose();
        Surface.Children.Remove(lunarScene);
        lunarScene = null;
    }
}
