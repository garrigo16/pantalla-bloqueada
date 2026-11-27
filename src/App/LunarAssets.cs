using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

// Autor: Miquel Garrigo
internal sealed class LunarFrame
{
    internal readonly byte[] Pixels;
    internal readonly int Width, Height, Bottom, InkWidth, InkHeight;
    internal readonly double Center;

    internal LunarFrame(byte[] pixels, int width, int height)
    {
        Pixels = pixels; Width = width; Height = height;
        int left = width, right = -1, top = height, bottom = -1;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (pixels[(y * width + x) * 4 + 3] != 0)
                {
                    left = Math.Min(left, x);
                    right = Math.Max(right, x);
                    top = Math.Min(top, y);
                    bottom = y;
                }
        if (bottom < 0) throw new InvalidDataException("Fotograma vacío.");
        Center = (left + right + 1) / 2.0;
        Bottom = bottom + 1;
        InkWidth = right - left + 1;
        InkHeight = bottom - top + 1;
    }
}

internal sealed class LunarClip
{
    internal readonly string Species, Name;
    internal readonly int Width, Height;
    internal readonly double[] FrameEnds;
    internal readonly double Duration;
    internal LunarFrame[][] Frames;

    internal LunarClip(string species, string name, int width, int height, params int[] ticks)
    {
        Species = species; Name = name; Width = width; Height = height;
        FrameEnds = new double[ticks.Length];
        for (int i = 0; i < ticks.Length; i++)
        {
            Duration += ticks[i] * 1000.0 / 60;
            FrameEnds[i] = Duration;
        }
    }

    internal LunarFrame At(int direction, double elapsed)
    {
        if (Frames == null) return null;
        var row = Frames[direction % Frames.Length];
        for (int i = 0; i < FrameEnds.Length - 1; i++)
            if (elapsed < FrameEnds[i]) return row[i];
        return row[row.Length - 1];
    }
}

internal static class LunarAssets
{
    internal const string Revision = "97494df8dfbb4ca74a243332ac3edf94e6dd95bf";
    private const int MaxSheetBytes = 128 * 1024;
    private const string Source = "https://cdn.jsdelivr.net/gh/PMDCollab/SpriteCollab@" + Revision + "/sprite/";
    private const string Fallback = "https://raw.githubusercontent.com/PMDCollab/SpriteCollab/" + Revision + "/sprite/";
    private static readonly Lazy<HttpClient> Client = new Lazy<HttpClient>(delegate
    {
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.UserAgent.ParseAdd("DrifftAnalyzer/" + ProductVersion.Current);
        return client;
    });

    internal static string CacheDirectory
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppIdentity.DirectoryName, "lunar-sprites", Revision); }
    }

    internal static async Task<LunarFrame[][]> Load(LunarClip clip, CancellationToken cancellation)
    {
        if (cancellation.IsCancellationRequested) return null;
        string file = Path.Combine(CacheDirectory, clip.Species + "-" + clip.Name + ".png");
        try
        {
            if (File.Exists(file) && new FileInfo(file).Length <= MaxSheetBytes)
            {
                using (var input = File.OpenRead(file)) return Decode(input, clip);
            }
        }
        catch (Exception e) { if (!Recoverable(e)) throw; }

        foreach (var source in new[] { Source, Fallback })
        {
            if (cancellation.IsCancellationRequested) return null;
            try
            {
                byte[] bytes = await Download(source + clip.Species + "/" + clip.Name + "-Anim.png", cancellation).ConfigureAwait(false);
                if (bytes == null) continue;
                LunarFrame[][] frames;
                using (var input = new MemoryStream(bytes, false)) frames = Decode(input, clip);
                if (!cancellation.IsCancellationRequested) Cache(file, bytes);
                return frames;
            }
            catch (Exception e) { if (!cancellation.IsCancellationRequested && !Recoverable(e)) throw; }
        }
        return null;
    }

    private static async Task<byte[]> Download(string source, CancellationToken cancellation)
    {
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
        {
            timeout.CancelAfter(TimeSpan.FromSeconds(8));
            using (var response = await Client.Value.GetAsync(source,
                HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > MaxSheetBytes) return null;
                using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var bytes = new MemoryStream())
                {
                    var chunk = new byte[4096];
                    int length;
                    while ((length = await input.ReadAsync(chunk, 0, chunk.Length, timeout.Token).ConfigureAwait(false)) > 0)
                    {
                        if (bytes.Length + length > MaxSheetBytes) return null;
                        bytes.Write(chunk, 0, length);
                    }
                    return bytes.ToArray();
                }
            }
        }
    }

    private static bool Recoverable(Exception e)
    {
        return e is IOException || e is InvalidDataException || e is UnauthorizedAccessException || e is FileFormatException
            || e is NotSupportedException || e is HttpRequestException || e is OperationCanceledException
            || e is FormatException || e is ArgumentException;
    }

    private static void Cache(string file, byte[] data)
    {
        string temp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllBytes(temp, data);
            if (File.Exists(file)) File.Replace(temp, file, null);
            else File.Move(temp, file);
        }
        catch (Exception e) { if (!Recoverable(e)) throw; }
        finally
        {
            try { File.Delete(temp); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static LunarFrame[][] Decode(Stream input, LunarClip clip)
    {
        var png = new PngBitmapDecoder(input, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var bitmap = new FormatConvertedBitmap(png.Frames[0], PixelFormats.Bgra32, null, 0);
        int rows = bitmap.PixelHeight / clip.Height;
        int columns = clip.FrameEnds.Length;
        if ((rows != 1 && rows != 8) || rows * clip.Height != bitmap.PixelHeight || bitmap.PixelWidth != clip.Width * columns)
            throw new InvalidDataException("Tamaño de hoja inesperado.");
        var result = new LunarFrame[rows][];
        for (int row = 0; row < rows; row++)
        {
            result[row] = new LunarFrame[columns];
            for (int column = 0; column < columns; column++)
            {
                int stride = clip.Width * 4;
                var pixels = new byte[clip.Height * stride];
                bitmap.CopyPixels(new Int32Rect(column * clip.Width, row * clip.Height, clip.Width, clip.Height), pixels, stride, 0);
                result[row][column] = new LunarFrame(pixels, clip.Width, clip.Height);
            }
        }
        return result;
    }
}

internal sealed class LunarBackdrop
{
    internal const int Width = 480, Height = 270;
    internal static readonly Lazy<LunarBackdrop> Resource = new Lazy<LunarBackdrop>(delegate { return new LunarBackdrop(); });
    internal readonly LunarBackdropMotion Motion;
    internal readonly Geometry VisitorClip;

    private LunarBackdrop()
    {
        BitmapSource image;
        using (var input = typeof(LunarBackdrop).Assembly.GetManifestResourceStream("LunarBackdrop.jpg"))
        {
            if (input == null) throw new InvalidDataException("Falta el fondo de la orilla.");
            image = BitmapFrame.Create(input, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            image.Freeze();
        }
        Motion = new LunarBackdropMotion(image);
        var bmp = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
        int stride = bmp.PixelWidth * 4;
        var pixels = new byte[stride * bmp.PixelHeight];
        bmp.CopyPixels(pixels, stride, 0);
        var corners = Geometry.Parse("M0 202H20L39 213L54 221L61 234L86 244L128 250V270H0Z M480 195H461L447 207L420 212L392 222L406 231L397 245L365 252L346 270H480Z");
        var clip = new StreamGeometry { FillRule = FillRule.EvenOdd };
        using (var path = clip.Open())
        {
            AddRectangle(path, 0, 0, Width, Height);
            for (int y = 195; y < Height; y++)
            {
                int start = -1;
                for (int x = 0; x <= Width; x++)
                {
                    int i = (int)((y + 0.5) * bmp.PixelHeight / Height) * stride
                        + 4 * (int)((x + 0.5) * bmp.PixelWidth / Width);
                    bool leaf = x < Width && pixels[i + 1] < 94 && pixels[i] - pixels[i + 1] >= 8
                        && pixels[i] - pixels[i + 2] >= 9 && corners.FillContains(new Point(x + 0.5, y + 0.5));
                    if (leaf && start == -1) start = x;
                    if (!leaf && start != -1)
                    {
                        AddRectangle(path, start, y, x - start, 1);
                        start = -1;
                    }
                }
            }
        }
        clip.Freeze();
        VisitorClip = clip;
    }

    private static void AddRectangle(StreamGeometryContext path, int x, int y, int width, int height)
    {
        path.BeginFigure(new Point(x, y), true, true);
        path.LineTo(new Point(x + width, y), false, false);
        path.LineTo(new Point(x + width, y + height), false, false);
        path.LineTo(new Point(x, y + height), false, false);
    }
}
