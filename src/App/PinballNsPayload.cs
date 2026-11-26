using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

// Autor: Miquel Garrigo
internal sealed class PinballNsPayload
{
    internal const string Version = "2.9";
    internal const string ExecutableName = "PinballNS.exe";
    private const int ArchiveBytes = 264649;
    private const string ArchiveHash = "0ed6770697229eb177e348d1567dde9127f2ab36a1dfdb312ba6314018a7e67a";
    private const string ExecutableHash = "067eb1938a87d177dfd9615da019d5e395e9328085b94dfcf9cfb0167a0a505e";
    private static readonly Uri DownloadUrl = new Uri(
        "https://github.com/ReiniShari/Pinball-NS/releases/download/v" + Version + "/PinballNS-Windows-" + Version + ".zip");
    private static readonly SemaphoreSlim SetupGate = new SemaphoreSlim(1, 1);
    private readonly string executable, rom;

    private PinballNsPayload(string engine, string path) { executable = engine; rom = path; }

    internal ProcessStartInfo StartInfo()
    {
        return new ProcessStartInfo(executable, "\"" + rom + "\"")
        {
            UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = Path.GetDirectoryName(executable)
        };
    }

    internal static async Task<PinballNsPayload> PrepareAsync(string path, CancellationToken cancellation)
    {
        if (!Environment.Is64BitOperatingSystem)
            throw new IOException("Pinball NS requiere Windows de 64 bits.");

        await SetupGate.WaitAsync(cancellation).ConfigureAwait(false);
        try
        {
            string root = PinballDownload.CheckedDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DrifftAnalyzer-PinballNS"));
            string directory = PinballDownload.CheckedDirectory(Path.Combine(root, Version));
            string executable = Path.Combine(directory, ExecutableName);
            if (!PinballPayload.Matches(executable, ExecutableHash))
            {
                using (var zip = await DownloadAsync(cancellation).ConfigureAwait(false))
                    Unpack(zip, executable);
            }
            cancellation.ThrowIfCancellationRequested();
            Mute(Path.Combine(directory, "pinball_ns.cfg"));
            return new PinballNsPayload(executable, Path.GetFullPath(path));
        }
        finally { SetupGate.Release(); }
    }

    private static void Mute(string path)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, "sound=0\r\n");
            return;
        }
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Los ajustes de Pinball NS no pueden ser un enlace.");
        string[] lines = File.ReadAllLines(path);
        for (int i = 0; i < lines.Length; i++)
        {
            if (!lines[i].StartsWith("sound=", StringComparison.Ordinal)) continue;
            if (lines[i] != "sound=0")
            {
                lines[i] = "sound=0";
                File.WriteAllLines(path, lines);
            }
            return;
        }
        File.AppendAllText(path, "\r\nsound=0\r\n");
    }

    internal static void Unpack(Stream zip, string executable)
    {
        if (zip.Length != ArchiveBytes || PinballPayload.Hash(zip) != ArchiveHash)
            throw new IOException("La descarga de Pinball NS no supera la comprobación de integridad.");
        zip.Position = 0;
        using (var archive = new ZipArchive(zip, ZipArchiveMode.Read, true))
        using (var input = archive.GetEntry(ExecutableName).Open())
            PinballPayload.Save(executable, input, ExecutableHash);
    }

    internal static Task<MemoryStream> DownloadAsync(CancellationToken cancellation)
    {
        return PinballDownload.FetchAsync(DownloadUrl, "Pinball NS", ArchiveBytes, cancellation);
    }
}
