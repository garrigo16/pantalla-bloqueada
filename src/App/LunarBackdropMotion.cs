using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

// Autor: Miquel Garrigo
internal sealed class LunarBackdropMotion
{
    private const int BreezeFrames = 17, WaterFrames = 28;
    private const double Turn = 2 * Math.PI;
    private static readonly Point[] PaintedLights =
    {
        new Point(58.4, 142.2), new Point(68.2, 134.7), new Point(445, 179.5),
        new Point(437.4, 201.2), new Point(58.9, 222)
    };
    private readonly BitmapSource baseImage;
    private readonly Patch leftTree, rightTree, water;

    private sealed class Patch
    {
        internal readonly BitmapSource[] Frames;
        internal readonly Rect Bounds;
        internal Patch(Rect bounds, int frames) { Bounds = bounds; Frames = new BitmapSource[frames]; }

        internal void Draw(DrawingContext dc, int frame) { dc.DrawImage(Frames[frame], Bounds); }
    }

    internal LunarBackdropMotion(BitmapSource original)
    {
        var bitmap = new FormatConvertedBitmap(original, PixelFormats.Bgra32, null, 0);
        int width = bitmap.PixelWidth, height = bitmap.PixelHeight;
        var pixels = new int[width * height];
        bitmap.CopyPixels(pixels, width * 4, 0);
        RemovePaintedLights(pixels, width, height);
        baseImage = Freeze(pixels, width, height);
        leftTree = CreatePatch(pixels, width, height, new Rect(47, 0, 169, 71), false);
        rightTree = CreatePatch(pixels, width, height, new Rect(323, 0, 157, 83), false);
        water = CreatePatch(pixels, width, height, new Rect(73, 125, 348, 50), true);
    }

    private static void RemovePaintedLights(int[] pixels, int width, int height)
    {
        double sx = (double)width / LunarBackdrop.Width, sy = (double)height / LunarBackdrop.Height;
        const double radius = 6.1;
        foreach (var center in PaintedLights)
        {
            // Clone the nearby texture into the small circle; the animated lights supply the glow.
            int shift = (int)Math.Round((center.X < 200 ? 15 : -12) * sx);
            for (int y = (int)((center.Y - radius) * sy); y <= (center.Y + radius) * sy; y++)
                for (int x = (int)((center.X - radius) * sx); x <= (center.X + radius) * sx; x++)
                {
                    double dx = x / sx - center.X, dy = y / sy - center.Y;
                    double distance = Math.Sqrt(dx * dx + dy * dy);
                    if (distance >= radius) continue;
                    int index = y * width + x;
                    double amount = Math.Min(1, (radius - distance) / 2.1);
                    pixels[index] = Mix(pixels[index], pixels[index + shift], amount);
                }
        }
    }

    private static Patch CreatePatch(int[] pixels, int width, int height, Rect area, bool isWater)
    {
        double sx = (double)width / LunarBackdrop.Width, sy = (double)height / LunarBackdrop.Height;
        int left = (int)Math.Round(area.Left * sx), top = (int)Math.Round(area.Top * sy);
        int columns = (int)Math.Round(area.Right * sx) - left, rows = (int)Math.Round(area.Bottom * sy) - top;
        int count = isWater ? WaterFrames : BreezeFrames;
        var patch = new Patch(new Rect(left / sx, top / sy, columns / sx, rows / sy), count);
        var frame = new int[columns * rows];
        for (int step = 0; step < count; step++)
        {
            double phase = Turn * step / count;
            double sway = 2.0 * step / (count - 1) - 1;
            for (int y = 0; y < rows; y++)
            {
                double v = isWater ? 2.0 * y / (rows - 1) - 1 : (double)y / (rows - 1);
                double wave = isWater ? Math.Sin(phase - y / sy * .22) : sway;
                double shine = isWater ? 7.5 * Math.Sin(phase - y / sy * .16 + .7) : 0;
                for (int x = 0; x < columns; x++)
                {
                    double u = 2.0 * x / (columns - 1) - 1;
                    double weight = Math.Max(0, (1 - u * u) * (1 - v * v));
                    weight *= weight;
                    double dx = wave * weight * (isWater ? 2.7 : 2.15);
                    double dy = weight * (isWater ? wave * .26 : sway * (.7 + .25 * u));
                    int px = Math.Max(0, Math.Min(width - 1, left + x + (int)Math.Round(dx * sx)));
                    int py = Math.Max(0, Math.Min(height - 1, top + y + (int)Math.Round(dy * sy)));
                    int colour = pixels[py * width + px];
                    if (isWater)
                    {
                        // Modulate the existing highlights, strongest in the bright reflection.
                        double reflection = Math.Min(1, Math.Max(0, (((colour >> 8) & 255) - 65) / 111.0));
                        int light = (int)Math.Round(shine * reflection * weight);
                        colour = Tint(colour, light);
                    }
                    frame[y * columns + x] = colour;
                }
            }
            patch.Frames[step] = Freeze(frame, columns, rows);
        }
        return patch;
    }

    internal void Draw(DrawingContext dc, Rect view, double seconds)
    {
        dc.DrawImage(baseImage, view);
        leftTree.Draw(dc, Breeze(seconds, 7.1, .2));
        rightTree.Draw(dc, Breeze(seconds, 8.7, 1.65));
        water.Draw(dc, (int)(seconds * WaterFrames / 5.4) % WaterFrames);
    }

    private static int Breeze(double seconds, double period, double offset)
    {
        double air = .79 * Math.Sin(seconds * Turn / period + offset) + .19 * Math.Sin(seconds * .51 + offset);
        return (int)Math.Round((air + 1) * (BreezeFrames - 1) / 2);
    }

    private static int Mix(int from, int to, double amount)
    {
        int colour = unchecked((int)0xff000000);
        for (int shift = 0; shift <= 16; shift += 8)
        {
            int first = (from >> shift) & 255, last = (to >> shift) & 255;
            colour |= ((int)Math.Round(first + (last - first) * amount)) << shift;
        }
        return colour;
    }

    private static int Tint(int colour, int light)
    {
        if (light == 0) return colour;
        int r = ClampByte(((colour >> 16) & 255) + light);
        int g = ClampByte(((colour >> 8) & 255) + light);
        int b = ClampByte((colour & 255) + light);
        return unchecked((int)0xff000000) | (r << 16) | (g << 8) | b;
    }

    private static int ClampByte(int value) { return Math.Max(0, Math.Min(255, value)); }

    private static BitmapSource Freeze(int[] pixels, int width, int height)
    {
        var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
        bitmap.Freeze();
        return bitmap;
    }
}

internal sealed class LunarFireflies
{
    private static readonly Brush Core = MakeCore();
    private static readonly Brush Glow = MakeGlow();
    private static readonly Rect[] Areas =
    {
        new Rect(30, 183, 63, 46), new Rect(47, 199, 60, 39),
        new Rect(113, 204, 89, 37), new Rect(189, 192, 68, 46),
        new Rect(272, 200, 92, 42), new Rect(367, 181, 70, 49), new Rect(409, 198, 43, 40)
    };
    private readonly Fly[] flies = new Fly[Areas.Length];
    private readonly Random random;

    private sealed class Fly
    {
        internal Rect Area;
        internal Point From, To;
        internal double Start, End, Phase, Speed, Radius, Height, Pulse;
    }

    internal LunarFireflies(Random random)
    {
        this.random = random;
        for (int i = 0; i < flies.Length; i++)
        {
            var fly = new Fly
            {
                Area = Areas[i], Phase = Between(0, 2 * Math.PI), Speed = Between(.55, 1.24) * (i % 2 == 0 ? 1 : -1),
                Radius = Between(3.8, 9.2), Height = Between(2.4, 5.2), Pulse = Between(1, 2.3),
                From = PointIn(Areas[i]), To = PointIn(Areas[i]), End = Between(6, 12)
            };
            flies[i] = fly;
        }
    }

    private double Between(double min, double max) { return min + (max - min) * random.NextDouble(); }

    private Point PointIn(Rect area) { return new Point(Between(area.Left, area.Right), Between(area.Top, area.Bottom)); }

    internal void Draw(DrawingContext dc, double seconds)
    {
        foreach (var fly in flies)
        {
            while (seconds >= fly.End)
            {
                fly.From = fly.To;
                fly.To = PointIn(fly.Area);
                fly.Start = fly.End;
                fly.End += Between(7, 15);
            }
            double ease = (seconds - fly.Start) / (fly.End - fly.Start);
            ease = ease * ease * (3 - 2 * ease);
            double phase = fly.Phase + seconds * fly.Speed;
            Point position = fly.From + (fly.To - fly.From) * ease +
                new Vector(Math.Cos(phase) * fly.Radius, Math.Sin(phase) * fly.Height);
            double brightness = .34 + .59 * (.5 + .5 * Math.Sin(fly.Phase + seconds * fly.Pulse));
            dc.PushOpacity(brightness);
            dc.DrawEllipse(Glow, null, position, 4, 4);
            dc.DrawRectangle(Core, null, new Rect(position.X - .7, position.Y - .7, 1.4, 1.4));
            dc.Pop();
        }
    }

    private static Brush MakeCore()
    {
        var brush = new SolidColorBrush(Color.FromRgb(246, 251, 153));
        brush.Freeze();
        return brush;
    }

    private static Brush MakeGlow()
    {
        var brush = new RadialGradientBrush(Color.FromArgb(134, 224, 252, 117), Color.FromArgb(0, 224, 252, 117));
        brush.Freeze();
        return brush;
    }
}
