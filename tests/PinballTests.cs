using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

internal static class PinballTests
{
    private static int count;
    private static void Check(bool ok, string message)
    { if (!ok) throw new Exception(message); count++; Console.WriteLine("OK " + message); }
    private static string Hash(Stream stream)
    { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
    private static string Field(object value, string name)
    { return (string)value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value); }
    private static int Main(string[] args)
    {
        try
        {
            var app = Assembly.LoadFile(Path.GetFullPath(args[0]));
            var payload = app.GetType("PinballPayload", true);
            Check((bool)payload.GetProperty("Available", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null, null), "La app incorpora el paquete real de Cat Pinball");
            string[] manifest;
            using (var input = app.GetManifestResourceStream("PinballManifest.txt"))
            using (var reader = new StreamReader(input)) manifest = reader.ReadToEnd().Trim().Split('\n');
            Check(manifest.Length == 2 && manifest[0].Trim().Length == 64 && manifest[1].Trim().Length == 64, "El manifiesto contiene las dos huellas SHA-256");
            using (var pack = app.GetManifestResourceStream("CatPinball.pck"))
            {
                Check(Hash(pack) == manifest[1].Trim(), "El paquete del juego coincide con el manifiesto");
                pack.Position = 0; var magic = new byte[4]; pack.Read(magic, 0, 4);
                Check(Encoding.ASCII.GetString(magic) == "GDPC", "El recurso es un paquete nativo de Godot");
            }
            using (var input = app.GetManifestResourceStream("Godot.zip"))
            {
                Check(Hash(input) == "731980f9608d61333e5baf54a2ef17210acc7a538446c0cb9969f002aca1e953", "El motor incluido coincide con el ZIP oficial de Godot");
                input.Position = 0;
                using (var zip = new ZipArchive(input, ZipArchiveMode.Read))
                using (var engine = zip.GetEntry("Godot_v4.7.2-stable_win64.exe").Open())
                    Check(Hash(engine) == manifest[0].Trim(), "El ejecutable del motor coincide con la huella verificada");
            }
            using (var input = app.GetManifestResourceStream("GodotLicense.txt"))
            using (var reader = new StreamReader(input))
                Check(reader.ReadToEnd().Contains("Permission is hereby granted"), "El paquete conserva la licencia del motor");
            var prepare = payload.GetMethod("Prepare", BindingFlags.Static | BindingFlags.NonPublic);
            object first = prepare.Invoke(null, null);
            string enginePath = Field(first, "Engine"), packPath = Field(first, "Pack"), statusPath = Field(first, "Status");
            Check(File.Exists(enginePath) && File.Exists(packPath), "Prepara ambos archivos antes de iniciar la partida");
            using (var input = File.OpenRead(enginePath)) Check(Hash(input) == manifest[0].Trim(), "La copia preparada del motor está íntegra");
            using (var input = File.OpenRead(packPath)) Check(Hash(input) == manifest[1].Trim(), "La copia preparada del juego está íntegra");
            Check(Path.GetFullPath(enginePath).StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase), "La caché queda en los temporales del usuario");
            Check(File.Exists(Path.Combine(Path.GetDirectoryName(enginePath), "Godot-LICENSE.txt")), "La licencia acompaña al motor preparado");
            DateTime written = File.GetLastWriteTimeUtc(enginePath);
            object second = prepare.Invoke(null, null);
            Check(Field(second, "Engine") == enginePath && File.GetLastWriteTimeUtc(enginePath) == written, "Reabrir el juego reutiliza la caché ya verificada");
            Check(Field(second, "Status") != statusPath, "Cada sesión usa un estado separado");
            Check(app.GetType("PinballPhysics") == null && app.GetType("PinballAmbient") == null, "Se retira el pinball anterior y su código duplicado");
            Console.WriteLine("Superadas " + count + " comprobaciones del paquete de Cat Pinball.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
