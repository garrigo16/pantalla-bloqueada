using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

// Autor: Miquel Garrigo
internal sealed class PinballPayload
{
    internal readonly string Engine, Pack, Status;
    private const string EngineName = "Godot_v4.7.2-stable_win64.exe";
    private const string OfficialZipHash = "731980f9608d61333e5baf54a2ef17210acc7a538446c0cb9969f002aca1e953";
    private PinballPayload(string directory)
    {
        Engine = Path.Combine(directory, EngineName); Pack = Path.Combine(directory, "CatPinball.pck");
        Status = Path.Combine(directory, "estado-" + Guid.NewGuid().ToString("N") + ".json");
    }
    internal static bool Available { get { return Assembly.GetExecutingAssembly().GetManifestResourceInfo("CatPinball.pck") != null; } }
    internal static string Hash(Stream input)
    { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant(); }
    internal static bool Matches(string path, string expected)
    {
        if (!File.Exists(path)) return false;
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("La caché del juego no puede contener enlaces.");
        using (var input = File.OpenRead(path)) return Hash(input) == expected;
    }
    internal static PinballPayload Prepare()
    {
        var assembly = Assembly.GetExecutingAssembly();
        if (!Available) throw new IOException("Esta compilación no incluye el paquete local de Cat Pinball.");
        string[] manifest;
        using (var input = assembly.GetManifestResourceStream("PinballManifest.txt"))
        using (var reader = new StreamReader(input)) manifest = reader.ReadToEnd().Trim().Split('\n');
        if (manifest.Length != 2) throw new IOException("El manifiesto del juego está incompleto.");
        string engineHash = manifest[0].Trim(), packHash = manifest[1].Trim();
        if (engineHash.Length != 64 || packHash.Length != 64) throw new IOException("El manifiesto del juego no es válido.");
        string root = Path.Combine(Path.GetTempPath(), "DrifftAnalyzer-pinball");
        Directory.CreateDirectory(root);
        if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0) throw new IOException("La caché del juego no puede ser un enlace.");
        string directory = Path.Combine(root, packHash.Substring(0, 20)); Directory.CreateDirectory(directory);
        if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) throw new IOException("La caché del juego no puede ser un enlace.");
        var result = new PinballPayload(directory);
        if (!Matches(result.Engine, engineHash))
        {
            using (var zip = assembly.GetManifestResourceStream("Godot.zip"))
            {
                if (Hash(zip) != OfficialZipHash) throw new IOException("El motor no coincide con la distribución oficial de Godot.");
                zip.Position = 0;
                using (var archive = new ZipArchive(zip, ZipArchiveMode.Read))
                using (var content = archive.GetEntry(EngineName).Open()) Save(result.Engine, content, engineHash);
            }
        }
        if (!Matches(result.Pack, packHash)) using (var content = assembly.GetManifestResourceStream("CatPinball.pck")) Save(result.Pack, content, packHash);
        string license = Path.Combine(directory, "Godot-LICENSE.txt");
        using (var content = assembly.GetManifestResourceStream("GodotLicense.txt"))
        {
            string licenseHash = Hash(content); content.Position = 0;
            if (!Matches(license, licenseHash)) Save(license, content, licenseHash);
        }
        return result;
    }
    internal static void Save(string path, Stream content, string hash)
    {
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var output = File.Create(temp)) content.CopyTo(output);
            if (!Matches(temp, hash)) throw new IOException("El archivo del juego no supera la comprobación de integridad.");
            if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
