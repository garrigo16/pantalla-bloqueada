using System;
using System.IO;
using System.Xml.Serialization;

internal static class EasterEggTests
{
    private static int checks;
    private static DateTime now = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
    private static HumorMode mode = HumorMode.Todos;
    private static EasterEggEngine New() { return new EasterEggEngine(delegate { return mode; }, delegate { return now; }); }
    private static void Check(bool value, string description)
    { if (!value) throw new Exception(description); checks++; Console.WriteLine("OK " + description); }
    private static bool Is(FunReaction value, FunKind kind) { return value != null && value.Kind == kind; }
    private static FunReaction Key(EasterEggEngine engine, FunKey key) { now = now.AddSeconds(3); return engine.Key(key, false, false); }
    private static int Main()
    {
        try
        {
            var serializer = new XmlSerializer(typeof(AppSettings));
            AppSettings initial;
            using (var bytes = new MemoryStream(AppSettings.DefaultXml())) initial = (AppSettings)serializer.Deserialize(bytes);
            Check(initial.Humor == HumorMode.Todos && initial.WakeOnInput && initial.TimeoutSeconds == 20 && initial.IdleSeconds == 300,
                "Instalador y app comparten los valores iniciales, con todos los eggs activos");
            using (var input = new StringReader("<AppSettings><TimeoutSeconds>73</TimeoutSeconds></AppSettings>")) initial = (AppSettings)serializer.Deserialize(input);
            Check(initial.Humor == HumorMode.Todos && initial.WakeOnInput && initial.TimeoutSeconds == 73, "Preferencias antiguas reciben las opciones nuevas sin perder las anteriores");
            initial.Humor = HumorMode.Discreto; initial.WakeOnInput = false;
            using (var output = new MemoryStream())
            { serializer.Serialize(output, initial); output.Position = 0; var saved = (AppSettings)serializer.Deserialize(output);
                Check(saved.Humor == HumorMode.Discreto && !saved.WakeOnInput, "Se conserva la elección de humor y despertar"); }

            var engine = New();
            Check(engine.Key(FunKey.Enter, true, false) == null && engine.Key(FunKey.Enter, false, true) == null && engine.Stats.Attempts == 0,
                "Tecla mantenida y combinaciones de comandos no producen reacciones ni intentos");
            Check(Is(Key(engine, FunKey.Escape), FunKind.Houdini), "Esc responde con Houdini");
            Check(Is(Key(engine, FunKey.Enter), FunKind.Stamp), "Enter muestra el sello de los tres cafés");
            Check(Is(Key(engine, FunKey.Space), FunKind.Sleepy), "Espacio produce la siesta del reloj");
            Check(Key(engine, FunKey.F1).Text.Contains("vacaciones"), "F1 tiene el departamento de ayuda de vacaciones");
            now = now.AddMilliseconds(700);
            Check(engine.Key(FunKey.Other, false, false) == null, "Enfría las reacciones ordinarias para evitar parpadeos");
            now = now.AddSeconds(3); engine.Reset();
            FunReaction reaction = null;
            for (int i = 1; i <= 10; i++)
            {
                now = now.AddMilliseconds(150); reaction = engine.Key(FunKey.Other, false, false);
                if (i == 3) Check(reaction.Text == "Frío, frío.", "Tercer intento rápido: Frío, frío");
                if (i == 6) Check(reaction.Text == "La desesperación no forma parte de los protocolos de seguridad", "Sexto intento: frase exacta elegida por el usuario");
            }
            Check(Is(reaction, FunKind.Embarrassed) && reaction.Text.Contains(EasterEggEngine.Desperation), "Décimo intento: cortina tímida y frase de desesperación");
            Check(engine.Stats.Attempts == 10 && engine.Stats.Bursts == 1 && engine.Stats.Summary().Contains("Diplomático"), "Los intentos alimentan logros privados de la sesión");
            engine.Reset(); now = now.AddSeconds(3);
            var sequence = new[] { FunKey.Up, FunKey.Up, FunKey.Down, FunKey.Down, FunKey.Left, FunKey.Right, FunKey.Left, FunKey.Right, FunKey.B, FunKey.A };
            engine.Key(FunKey.Up, false, false);
            foreach (var key in sequence) { now = now.AddMilliseconds(100); reaction = engine.Key(key, false, false); }
            Check(Is(reaction, FunKind.Konami) && engine.Stats.Konami == 1, "Konami reconoce el prefijo solapado y muestra la mascota");
            engine.Reset();
            engine.Key(FunKey.Up, false, false); engine.Key(FunKey.Other, false, false);
            for (int i = 1; i < sequence.Length; i++) engine.Key(sequence[i], false, false);
            Check(engine.Stats.Konami == 0, "Una tecla incorrecta interrumpe Konami");
            engine.Reset();
            Check(engine.Text("ABRACADABRA", true) == null && engine.Stats.Magic == 0, "La palabra mágica ignora las combinaciones de comandos");
            Check(Is(engine.Text("aabracadabra", false), FunKind.Magic), "Abracadabra admite composición y prefijos solapados");
            engine.Reset(); engine.Text("abra", false); Key(engine, FunKey.Enter);
            Check(engine.Text("cadabra", false) == null, "Una tecla de control interrumpe la palabra mágica");
            engine.Reset(); now = now.AddSeconds(3);
            Check(Is(engine.Click(1, FunTarget.Clock), FunKind.Evade), "El reloj esquiva el clic");
            now = now.AddSeconds(3); Check(Is(engine.Click(1, FunTarget.Background), FunKind.Shake), "El fondo hace negar al reloj");
            Check(Is(engine.Click(3, FunTarget.Background), FunKind.PostIt), "Triple clic muestra el post-it");
            Check(Is(engine.Click(1, FunTarget.Network), FunKind.Networks), "El icono de red muestra redes ficticias");
            engine.Reset();
            for (int i = 0; i < 100; i++) CheckMoveOnePixel(engine);
            Check(engine.Stats.Attempts == 0, "El movimiento sintético de un píxel no activa bromas ni contadores");
            Check(engine.Move(50, false) == null, "Los ojos no aparecen sobre un fondo iluminado");
            now = now.AddSeconds(3); Check(engine.Move(20, true) == null, "Un movimiento corto no basta para los ojos");
            now = now.AddMilliseconds(100); Check(Is(engine.Move(20, true), FunKind.Eyes), "40 píxeles en 400 ms muestran los ojos");
            now = now.AddSeconds(3); engine.Move(20, true); now = now.AddSeconds(1);
            Check(engine.Move(20, true) == null, "Los movimientos separados no se acumulan indefinidamente");
            Check(engine.Pixel(false) == null, "El guiño de un píxel nunca enciende un fondo negro");
            now = now.AddSeconds(3); Check(Is(engine.Pixel(true), FunKind.Pixel), "El guiño de un píxel aparece si ya está visible el fondo");

            mode = HumorMode.Discreto; engine.Reset();
            foreach (var key in sequence) CheckSilent(engine.Key(key, false, false));
            Check(engine.Click(3, FunTarget.Background) == null && engine.Text("abracadabra", false) == null && engine.Move(100, true) == null && engine.Pixel(true) == null,
                "Discreto silencia textos y animaciones aunque se reconozcan gestos");
            mode = HumorMode.Sarcasmo; engine.Reset();
            Check(engine.Move(100, true) == null && Is(Key(engine, FunKey.Enter), FunKind.Stamp), "Sarcasmo filtra las animaciones sin mensaje");
            mode = HumorMode.Mascota; engine.Reset();
            Check(Key(engine, FunKey.Other) == null && Is(engine.Move(100, true), FunKind.Eyes), "Mascota filtra las frases y conserva los ojos");
            mode = HumorMode.Todos; engine.Reset();
            Check(engine.Stats.Attempts == 0 && engine.Stats.Clicks == 0 && engine.Stats.Magic == 0 && engine.Stats.Konami == 0, "La nueva simulación reinicia los contadores en memoria");
            Check(EasterEggEngine.CalendarSuffix(new DateTime(2026, 11, 23), mode).Contains("ánimo"), "Calendario del lunes");
            Check(EasterEggEngine.CalendarSuffix(new DateTime(2026, 11, 20, 13, 59, 0), mode) == "" && EasterEggEngine.CalendarSuffix(new DateTime(2026, 11, 20, 14, 0, 0), mode).Contains("casi"), "Calendario del viernes a partir de las 14");
            Check(EasterEggEngine.CalendarSuffix(new DateTime(2026, 12, 24), mode).Contains("descansa") && EasterEggEngine.CalendarSuffix(new DateTime(2026, 12, 28), mode).Contains("inocente"), "Guiños de Nochebuena y los Inocentes");
            Check(EasterEggEngine.CalendarSuffix(new DateTime(2026, 11, 23), HumorMode.Discreto) == "", "Discreto conserva la fecha sin guiños");
            Console.WriteLine("Superadas " + checks + " comprobaciones de easter eggs."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static void CheckMoveOnePixel(EasterEggEngine engine) { if (engine.Move(1, true) != null) throw new Exception("Reacción al píxel sintético"); }
    private static void CheckSilent(FunReaction reaction) { if (reaction != null) throw new Exception("Reacción en modo discreto"); }
}
