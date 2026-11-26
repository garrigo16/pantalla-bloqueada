using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

internal sealed class TestInstallHost : IInstallHost
{
    internal string Value;
    internal bool FailLaunch, FailRegistration;
    internal int Stops, Starts;
    internal string Started;
    public string StartupCommand
    {
        get { return Value; }
        set
        {
            if (FailRegistration) { FailRegistration = false; throw new IOException("Registro rechazado para prueba"); }
            Value = value;
        }
    }
    public void StopApplication() { Stops++; }
    public void Launch(string executable)
    {
        if (!File.Exists(executable)) throw new Exception("Falta el ejecutable antes de arrancar.");
        if (!File.Exists(Path.Combine(Path.GetDirectoryName(executable), "ajustes.xml"))) throw new Exception("Falta la configuración.");
        if (FailLaunch) throw new IOException("Inicio rechazado para prueba");
        Started = executable;
        Starts++;
    }
}

internal static class PackageTests
{
    private static readonly List<string> results = new List<string>();
    private static void Check(bool value, string description)
    {
        if (!value) throw new Exception(description);
        results.Add("OK " + description);
    }
    private static string Digest(byte[] bytes)
    {
        using (var hash = SHA256.Create()) return Convert.ToBase64String(hash.ComputeHash(bytes));
    }
    private static byte[] Resource(Assembly assembly, string name)
    {
        using (var input = assembly.GetManifestResourceStream(name))
        using (var output = new MemoryStream()) { input.CopyTo(output); return output.ToArray(); }
    }

    private static int Main(string[] args)
    {
        string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "paquete-" + Guid.NewGuid().ToString("N").Substring(0, 8));
        string report = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resultado-paquete.txt");
        try
        {
            var assembly = Assembly.LoadFile(Path.GetFullPath(args[0]));
            byte[] app = Resource(assembly, "Application.exe");
            byte[] remove = Resource(assembly, "Uninstaller.exe");
            Check(Digest(app) == Digest(File.ReadAllBytes(args[1])), "El instalador contiene exactamente la app verificada");
            Check(Digest(remove) == Digest(File.ReadAllBytes(args[2])), "El desinstalador está incluido en el instalador");
            string initialPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[0])), "Iniciar.exe");
            Check(new FileInfo(initialPath).Length < 64L * 1024 * 1024, "El instalador inicial cabe en el límite de los actualizadores antiguos");
            var initial = Assembly.LoadFile(initialPath).GetType("BootstrapPackage", true);
            string fullHash;
            using (var input = File.OpenRead(args[0]))
            using (var sha = SHA256.Create()) fullHash = BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "");
            Check(fullHash.Equals((string)initial.GetField("Sha256", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue(), StringComparison.OrdinalIgnoreCase) &&
                new FileInfo(args[0]).Length == (long)initial.GetField("Size", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue(),
                "El instalador inicial incorpora el tamaño y la huella exactos del paquete completo probado");
            var appAssembly = Assembly.LoadFile(Path.GetFullPath(args[1]));
            Check(Resource(appAssembly, "LunarBackdrop.jpg").Length > 0, "El instalador incluye el paisaje de la orilla");
            Check(((AssemblyProductAttribute)Attribute.GetCustomAttribute(appAssembly, typeof(AssemblyProductAttribute))).Product == "Drifft Analyzer" &&
                ((AssemblyTitleAttribute)Attribute.GetCustomAttribute(appAssembly, typeof(AssemblyTitleAttribute))).Title == "Drifft Analyzer",
                "Windows muestra Drifft Analyzer como producto y descripción");
            string target = Path.Combine(root, "Perfil con espacios y acentos á", "PantallaBloqueada");
            var install = new PackageInstallation(target, app, remove);
            var host = new TestInstallHost();
            install.Install(host);
            Check(Path.GetFileName(install.ExecutablePath) == "DrifftAnalyzer.exe", "El proceso utiliza el ejecutable DrifftAnalyzer.exe");
            Check(Digest(File.ReadAllBytes(install.ExecutablePath)) == Digest(app), "Instalación en una ruta de otro usuario con espacios y acentos");
            Check(host.Value == "\"" + install.ExecutablePath + "\" --background", "Inicio automático usa la ruta local del destinatario, entre comillas");
            Check(host.Starts == 1 && host.Stops == 1 && host.Started == install.ExecutablePath, "Inicio de la copia instalada tras preparar archivos y configuración");
            var xml = new XmlDocument();
            xml.Load(Path.Combine(target, "ajustes.xml"));
            Check(xml.SelectSingleNode("/AppSettings/DeviceName").InnerText == "" && xml.SelectSingleNode("/AppSettings/ImagePath").InnerText == "",
                "No incluye la pantalla ni rutas de imágenes personales");
            Check(xml.SelectSingleNode("/AppSettings/TimeoutSeconds").InnerText == "20" && xml.SelectSingleNode("/AppSettings/ActivityIntervalSeconds").InnerText == "300",
                "Valores iniciales: 20 segundos y 5 minutos");
            Check(xml.SelectSingleNode("/AppSettings/KeepAwake").InnerText == "true" && xml.SelectSingleNode("/AppSettings/SimulateActivity").InnerText == "true",
                "Actividad periódica y petición de energía habilitadas");
            Check(xml.SelectSingleNode("/AppSettings/AutoActivate").InnerText == "true" && xml.SelectSingleNode("/AppSettings/IdleSeconds").InnerText == "300",
                "El instalador incluye la activación por inactividad a los 300 segundos");
            Check(xml.SelectSingleNode("/AppSettings/Humor").InnerText == "Todos" && xml.SelectSingleNode("/AppSettings/WakeOnInput").InnerText == "true",
                "El instalador incluye todos los easter eggs y el despertar breve");
            Check(xml.SelectSingleNode("/AppSettings/Theme").InnerText == "Clasico" &&
                xml.SelectSingleNode("/AppSettings/LunarDelaySeconds").InnerText == "5" &&
                xml.SelectSingleNode("/AppSettings/PokemonRomPath").InnerText == "" &&
                xml.SelectSingleNode("/AppSettings/PokemonRubyRomPath").InnerText == "",
                "El instalador parte de Clásico y conserva la espera inicial de cinco segundos para Pokémon");
            var serializer = new XmlSerializer(typeof(AppSettings));
            using (var old = new StringReader("<AppSettings><ShowLunarScene>true</ShowLunarScene><TimeoutSeconds>47</TimeoutSeconds><LunarDelaySeconds>11</LunarDelaySeconds></AppSettings>"))
            {
                var previous = (AppSettings)serializer.Deserialize(old);
                Check(previous.Theme == AppTheme.Clasico && previous.LunarDelaySeconds == 11 && previous.TimeoutSeconds == 47,
                    "El antiguo interruptor da paso al tema Clásico y se conservan ambos tiempos");
            }
            using (var output = new MemoryStream())
            {
                var configured = new AppSettings { Theme = AppTheme.Pokemon, LunarDelaySeconds = 0,
                    PokemonRomPath = @"C:\Juegos privados\Mi cartucho.gbc", PokemonRubyRomPath = @"C:\Juegos privados\Rubí.gba" };
                serializer.Serialize(output, configured);
                output.Position = 0;
                var restored = (AppSettings)serializer.Deserialize(output);
                Check(restored.Theme == AppTheme.Pokemon && restored.LunarDelaySeconds == 0 && restored.PokemonRomPath == configured.PokemonRomPath && restored.PokemonRubyRomPath == configured.PokemonRubyRomPath,
                    "Se guardan el tema Pokémon, la espera de cero segundos y las rutas locales de ambos juegos");
            }

            xml.SelectSingleNode("/AppSettings/TimeoutSeconds").InnerText = "75";
            xml.Save(Path.Combine(target, "ajustes.xml"));
            byte[] customSettings = File.ReadAllBytes(Path.Combine(target, "ajustes.xml"));
            File.WriteAllText(Path.Combine(target, "mi-imagen-personal.txt"), "Conservar");
            install.Install(host);
            Check(Digest(File.ReadAllBytes(Path.Combine(target, "ajustes.xml"))) == Digest(customSettings), "Reinstalar conserva los ajustes del usuario");
            Check(File.ReadAllText(Path.Combine(target, "mi-imagen-personal.txt")) == "Conservar", "Reinstalar conserva archivos ajenos al paquete");
            install.Uninstall(host);
            Check(host.Value == null && !File.Exists(install.ExecutablePath) && !File.Exists(Path.Combine(target, "Desinstalar.exe")),
                "Desinstalar retira ejecutables e inicio automático");
            Check(File.Exists(Path.Combine(target, "mi-imagen-personal.txt")), "Desinstalar conserva archivos personales y su carpeta");

            string fresh = Path.Combine(root, "fallo inicio", "PantallaBloqueada");
            var failing = new PackageInstallation(fresh, app, remove);
            var badHost = new TestInstallHost { Value = "entrada anterior", FailLaunch = true };
            try { failing.Install(badHost); throw new Exception("Debía fallar el inicio."); } catch (IOException) { }
            Check(!Directory.Exists(fresh) && badHost.Value == "entrada anterior", "Un fallo de inicio revierte archivos nuevos e inicio automático");

            string updated = Path.Combine(root, "fallo actualización", "PantallaBloqueada");
            var update = new PackageInstallation(updated, app, remove);
            var updateHost = new TestInstallHost();
            update.Install(updateHost);
            byte[] previousBinary = Encoding.ASCII.GetBytes("MZ versión anterior");
            File.WriteAllBytes(update.ExecutablePath, previousBinary);
            updateHost.FailRegistration = true;
            try { update.Install(updateHost); throw new Exception("Debía fallar el registro."); } catch (IOException) { }
            Check(Digest(File.ReadAllBytes(update.ExecutablePath)) == Digest(previousBinary), "Un fallo durante la actualización recupera el ejecutable anterior");
            Check(updateHost.Starts == 2 && updateHost.Started == update.ExecutablePath, "Al revertir la actualización vuelve a iniciar la versión anterior");
            string blockedDirectory = Path.Combine(root, "carpeta bloqueada", "PantallaBloqueada");
            Directory.CreateDirectory(Path.GetDirectoryName(blockedDirectory));
            File.WriteAllText(blockedDirectory, "No es una carpeta de instalación");
            var blockedHost = new TestInstallHost();
            try { new PackageInstallation(blockedDirectory, app, remove).Install(blockedHost, true); throw new Exception("Debía fallar la preparación."); } catch (IOException) { }
            Check(blockedHost.Stops == 0 && blockedHost.Starts == 0, "Si no puede preparar la carpeta, conserva la app anterior abierta");
            updateHost.Value = "otra aplicación";
            update.Uninstall(updateHost);
            Check(updateHost.Value == "otra aplicación" && !Directory.Exists(updated), "No elimina entradas ajenas y retira solo una carpeta vacía de la app");
            string migrated = Path.Combine(root, "migrar", "PantallaBloqueada");
            string legacySettings = Path.Combine(root, "ajustes-anteriores.xml");
            File.WriteAllBytes(legacySettings, customSettings);
            var migration = new PackageInstallation(migrated, app, remove);
            var disabledStartup = new TestInstallHost();
            migration.Install(disabledStartup, true, legacySettings);
            Check(disabledStartup.Value == null && Digest(File.ReadAllBytes(Path.Combine(migrated, "ajustes.xml"))) == Digest(customSettings),
                "Actualizar migra los ajustes de la copia anterior y conserva el inicio automático desactivado");
            disabledStartup.Value = "\"C:\\Portable\\PantallaBloqueada.exe\" --background";
            migration.Install(disabledStartup, true, legacySettings);
            Check(disabledStartup.Value == migration.ExpectedStartup, "Actualizar mantiene activado el inicio automático usando la nueva ubicación");
            File.WriteAllText(Path.Combine(migrated, "updates.xml"), "caché de prueba");
            migration.Uninstall(disabledStartup);
            Check(!Directory.Exists(migrated), "Desinstalar también retira la caché del actualizador");

            string renamedDirectory = Path.Combine(root, "cambiar nombre", "PantallaBloqueada");
            Directory.CreateDirectory(renamedDirectory);
            string legacyExecutable = Path.Combine(renamedDirectory, "PantallaBloqueada.exe");
            string legacyCommand = "\"" + legacyExecutable + "\" --background";
            File.WriteAllBytes(legacyExecutable, previousBinary);
            File.WriteAllBytes(Path.Combine(renamedDirectory, "ajustes.xml"), customSettings);
            var renamed = new PackageInstallation(renamedDirectory, app, remove);
            var renameHost = new TestInstallHost { Value = legacyCommand };
            renamed.Install(renameHost, true);
            Check(!File.Exists(legacyExecutable) && File.Exists(renamed.ExecutablePath) && renameHost.Value == renamed.ExpectedStartup,
                "Migrar una copia antigua retira el ejecutable anterior y actualiza el inicio automático");
            Check(Digest(File.ReadAllBytes(Path.Combine(renamedDirectory, "ajustes.xml"))) == Digest(customSettings), "Cambiar el nombre conserva los ajustes byte por byte");
            renamed.Install(renameHost, true);
            Check(renameHost.Value == renamed.ExpectedStartup, "Una actualización posterior reconoce el nuevo nombre del ejecutable");
            renamed.Uninstall(renameHost);
            Check(!Directory.Exists(renamedDirectory) && renameHost.Value == null, "Desinstalar la copia renombrada retira sus archivos e inicio automático");

            Directory.CreateDirectory(renamedDirectory);
            File.WriteAllBytes(legacyExecutable, previousBinary);
            File.WriteAllBytes(Path.Combine(renamedDirectory, "ajustes.xml"), customSettings);
            renameHost.Value = legacyCommand; renameHost.FailLaunch = true;
            try { renamed.Install(renameHost, true); throw new Exception("Debía fallar el inicio nuevo."); } catch (IOException) { }
            Check(File.Exists(legacyExecutable) && !File.Exists(renamed.ExecutablePath) && Digest(File.ReadAllBytes(legacyExecutable)) == Digest(previousBinary) && renameHost.Value == legacyCommand,
                "Si falla la migración recupera el ejecutable y el inicio anteriores");
            renamed.Uninstall(renameHost);
            Check(!Directory.Exists(renamedDirectory) && renameHost.Value == null, "El desinstalador nuevo también puede retirar una instalación con el nombre antiguo");
            Check(!StartupRegistration.IsAppCommand("\"C:\\Tools\\OtraDrifftAnalyzer.exe\" --background") &&
                !StartupRegistration.IsAppCommand("\"C:\\Tools\\DrifftAnalyzer.exe\" --otro") &&
                StartupRegistration.IsAppCommand("\"C:\\Perfil con espacios\\DrifftAnalyzer.exe\" --background"),
                "Reconoce solo los nombres y argumentos propios en el inicio automático");
            try { new PackageInstallation(Path.GetPathRoot(root), app, remove); throw new Exception("Debía rechazar la raíz."); }
            catch (IOException) { results.Add("OK Rechaza instalar o borrar en la raíz de una unidad"); }
            File.WriteAllLines(report, results);
            foreach (string line in results) System.Console.WriteLine(line);
            return 0;
        }
        catch (Exception ex)
        {
            results.Add("ERROR " + ex.ToString());
            File.WriteAllLines(report, results);
            System.Console.WriteLine(ex);
            return 1;
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
