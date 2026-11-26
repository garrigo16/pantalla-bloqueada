using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

// Autor: Miquel Garrigo
internal static class PokemonPinballProcess
{
    private const string RubyVersion = "0.3.0";
    private const string RubyArchive = "JVPPBPort-" + RubyVersion + "-windows";
    private const string RubyExe = "JVPPBPort.exe";
    private const string RubyZipHash = "43b8d00ccb13491e54fed35a63e0e89c90cd336a2bfb1fc0fd9b26bf1d09180a";
    private const string RubyExeHash = "9cd9d9936bb56072cc5f12a43c9b13beb0a035023785c12569aaa3f7a6ce0a58";
    private const int RubyBytes = 1347964;
    private const string MgbaVersion = "0.10.5";
    private const string MgbaArchive = "mGBA-" + MgbaVersion + "-win64";
    private const string MgbaExe = "mgba-sdl.exe";
    private const string MgbaArchiveHash = "b497a57c7d9093834dadc64f33a90f7c411439c21fdb8a0143255a45ea37563a";
    private const string MgbaExeHash = "e08d9771033acd9a518175ef528ad3a54b6d2a2e532c7aaf3af54b7ed5af420f";
    private const int MgbaBytes = 14156919;
    private static readonly Uri RubyUrl = new Uri("https://github.com/jonnisvibes/jvppbport/releases/download/v" + RubyVersion + "/" + RubyArchive + ".zip");
    private static readonly Uri MgbaUrl = new Uri("https://github.com/mgba-emu/mgba/releases/download/" + MgbaVersion + "/" + MgbaArchive + ".7z");
    private static readonly SemaphoreSlim Setup = new SemaphoreSlim(1, 1);

    internal static async Task<ProcessStartInfo> PrepareAsync(PokemonPinballEngine engine, string rom, CancellationToken cancellation)
    {
        if (!Environment.Is64BitOperatingSystem) throw new IOException("El motor del juego requiere Windows de 64 bits.");
        bool ruby = engine == PokemonPinballEngine.JvppbPort;
        string fullRom = Path.GetFullPath(rom);
        await Setup.WaitAsync(cancellation).ConfigureAwait(false);
        try
        {
            string root = PinballDownload.CheckedDirectory(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DrifftAnalyzer-Pinball"));
            string directory = PinballDownload.CheckedDirectory(Path.Combine(root, ruby ? RubyArchive : MgbaArchive));
            string executable = Path.Combine(directory, ruby ? RubyExe : MgbaExe);
            if (!PinballPayload.Matches(executable, ruby ? RubyExeHash : MgbaExeHash))
            {
                if (ruby)
                {
                    using (var zip = await PinballDownload.FetchAsync(RubyUrl, "JVPPBPort", RubyBytes, cancellation).ConfigureAwait(false))
                        UnpackRuby(zip, executable);
                }
                else
                {
                    using (var archive = await PinballDownload.FetchAsync(MgbaUrl, "mGBA", MgbaBytes, cancellation).ConfigureAwait(false))
                        UnpackMgba(archive, executable);
                }
            }

            cancellation.ThrowIfCancellationRequested();
            return ruby ? RubyStart(executable, fullRom) : MgbaStart(executable, fullRom);
        }
        finally { Setup.Release(); }
    }

    internal static ProcessStartInfo RubyStart(string executable, string rom)
    {
        string directory = Path.GetDirectoryName(executable);
        var info = Start(executable, "");
        info.EnvironmentVariables["JVPPBPORT_ROM"] = rom;
        info.EnvironmentVariables["JVPPBPORT_MUTE"] = "1";
        info.EnvironmentVariables["JVPPBPORT_DATA_DIR"] = directory;
        var bindings = Path.Combine(directory, "jvppbport.ini");
        if (!File.Exists(bindings))
            File.WriteAllText(bindings, "[PinballKeys]\r\nset=1\r\nlaunch_set=1\r\nleft_flipper=Left Ctrl\r\nright_flipper=Right Ctrl\r\nlaunch=Space\r\ntilt_left=X\r\ntilt_right=.\r\ntilt_up=Up\r\n");
        return info;
    }

    internal static ProcessStartInfo MgbaStart(string executable, string rom)
    {
        string directory = Path.GetDirectoryName(executable);
        var portable = Path.Combine(directory, "portable.ini");
        if (!File.Exists(portable)) File.WriteAllText(portable, "");
        // mGBA SDL separa el directorio de guardado por barras normales.
        var info = Start(executable, "-3 -C mute=1 -C volume=0 \"" + rom.Replace('\\', '/') + "\"");
        info.EnvironmentVariables["SDL_AUDIODRIVER"] = "dummy";
        return info;
    }

    private static ProcessStartInfo Start(string executable, string args)
    {
        return new ProcessStartInfo(executable, args)
        {
            UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = Path.GetDirectoryName(executable)
        };
    }

    internal static void UnpackRuby(Stream zip, string executable)
    {
        Verify(zip, RubyBytes, RubyZipHash, "JVPPBPort");
        using (var archive = new ZipArchive(zip, ZipArchiveMode.Read, true))
        using (var entry = archive.GetEntry(RubyArchive + "/" + RubyExe).Open())
            PinballPayload.Save(executable, entry, RubyExeHash);
    }

    internal static void UnpackMgba(Stream archive, string executable)
    {
        Verify(archive, MgbaBytes, MgbaArchiveHash, "mGBA");
        var tar = Path.Combine(Environment.SystemDirectory, "tar.exe");
        if (!File.Exists(tar)) throw new IOException("Windows no dispone de tar.exe para preparar mGBA.");
        var temporary = Path.Combine(Path.GetDirectoryName(executable), Guid.NewGuid().ToString("N") + ".7z");
        try
        {
            using (var output = File.Create(temporary)) archive.CopyTo(output);
            var info = Start(tar, "-xOf \"" + temporary + "\" " + MgbaArchive + "/" + MgbaExe);
            info.RedirectStandardOutput = true;
            using (var extraction = Process.Start(info))
            {
                PinballPayload.Save(executable, extraction.StandardOutput.BaseStream, MgbaExeHash);
                if (!extraction.WaitForExit(5000) || extraction.ExitCode != 0)
                    throw new IOException("No se pudo extraer el ejecutable oficial de mGBA.");
            }
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static void Verify(Stream archive, int size, string hash, string name)
    {
        if (archive.Length != size || PinballPayload.Hash(archive) != hash)
            throw new IOException("La descarga de " + name + " no supera la comprobación de integridad.");
        archive.Position = 0;
    }
}
