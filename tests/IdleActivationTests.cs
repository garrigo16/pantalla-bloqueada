using System;
using System.IO;
using System.Xml.Serialization;

internal sealed class StubIdleInput : IIdleInputSource
{
    internal IdleSample Sample;
    internal bool Readable = true;
    internal int Reads;
    public bool TryRead(out IdleSample sample) { Reads++; sample = Sample; return Readable; }
}

internal static class IdleActivationTests
{
    private static int checks;
    private static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
        checks++; Console.WriteLine("OK " + description);
    }
    private static int Main()
    {
        try
        {
            var defaults = new AppSettings();
            Check(defaults.AutoActivate && defaults.IdleSeconds == 300, "Activación automática configurada inicialmente a 300 segundos");
            var serializer = new XmlSerializer(typeof(AppSettings));
            AppSettings legacy;
            using (var input = new StringReader("<AppSettings><TimeoutSeconds>75</TimeoutSeconds><ShowClock>false</ShowClock></AppSettings>"))
                legacy = (AppSettings)serializer.Deserialize(input);
            Check(legacy.AutoActivate && legacy.IdleSeconds == 300 && legacy.TimeoutSeconds == 75 && !legacy.ShowClock,
                "Las preferencias antiguas reciben la nueva opción sin perder sus valores");
            defaults.AutoActivate = false; defaults.IdleSeconds = 95;
            using (var text = new StringWriter())
            {
                serializer.Serialize(text, defaults);
                using (var input = new StringReader(text.ToString()))
                {
                    var restored = (AppSettings)serializer.Deserialize(input);
                    Check(!restored.AutoActivate && restored.IdleSeconds == 95, "Se conservan el tiempo elegido y la casilla desactivada");
                }
            }
            Check(SystemIdleInput.ElapsedMilliseconds(110, 10) == 100, "Calcula la inactividad desde la última entrada");
            Check(SystemIdleInput.ElapsedMilliseconds(10, uint.MaxValue - 9) == 20, "Tolera la vuelta a cero del contador de Windows");
            Check(SystemIdleInput.ElapsedMilliseconds(100, 110) == 0, "Una marca futura de entrada no provoca una activación falsa");

            var policy = new IdleActivationPolicy();
            Check(!policy.ShouldActivate(new IdleSample(10, 299999), 300), "No activa antes de cumplirse el tiempo");
            Check(policy.ShouldActivate(new IdleSample(10, 300000), 300), "Activa cuando se cumple el tiempo exacto");
            Check(!policy.ShouldActivate(new IdleSample(10, uint.MaxValue), 0) && !policy.ShouldActivate(new IdleSample(10, uint.MaxValue), 86401), "Rechaza umbrales fuera del rango permitido");
            policy.DeferUntilActivity(new IdleSample(10, 300000));
            Check(!policy.ShouldActivate(new IdleSample(10, 900000), 300), "Salir manualmente evita reactivarse con la misma entrada antigua");
            Check(!policy.ShouldActivate(new IdleSample(20, 0), 300), "Una entrada nueva reinicia la espera");
            Check(policy.ShouldActivate(new IdleSample(20, 300000), 300), "Tras usar otra vez el PC vuelve a activar al llegar al umbral");
            policy.DeferUntilActivity(null);
            Check(!policy.ShouldActivate(new IdleSample(30, 900000), 300) && !policy.ShouldActivate(new IdleSample(30, 1000000), 300),
                "Si no se pudo leer la entrada al salir, espera una actividad nueva");
            policy.Reset();
            Check(policy.ShouldActivate(new IdleSample(30, 300000), 300), "Restablece la espera después de cambiar ajustes o volver a la sesión");

            var settings = new AppSettings { IdleSeconds = 10 };
            var source = new StubIdleInput { Sample = new IdleSample(10, 9999) };
            bool active = false, configuring = false, locked = false;
            int activations = 0;
            using (var monitor = new IdleActivationMonitor(source, delegate { return settings; },
                delegate { return !active && !configuring && !locked; }, delegate { active = true; activations++; }))
            {
                monitor.Poll(); Check(activations == 0, "El monitor no anticipa la activación");
                source.Sample = new IdleSample(10, 10000);
                monitor.Poll(); Check(activations == 1 && active, "El monitor activa la simulación una vez al alcanzar X segundos");
                int reads = source.Reads;
                source.Sample = new IdleSample(11, 0); monitor.Poll();
                Check(activations == 1 && source.Reads == reads, "Mientras está activa no mide sus movimientos sintéticos ni vuelve a activar");
                active = false; monitor.DeferUntilActivity();
                source.Sample = new IdleSample(11, 900000); monitor.Poll();
                Check(activations == 1, "La salida manual no crea un bucle de reapertura");
                source.Sample = new IdleSample(12, 0); monitor.Poll();
                source.Sample = new IdleSample(12, 10000); monitor.Poll();
                Check(activations == 2, "Una segunda pausa tras tocar el PC vuelve a funcionar");
                active = false; monitor.Reset();
                configuring = true; reads = source.Reads; monitor.Poll();
                Check(activations == 2 && source.Reads == reads, "No interrumpe el configurador abierto");
                configuring = false; locked = true; monitor.Poll();
                Check(activations == 2 && source.Reads == reads, "No activa sobre una sesión bloqueada o desconectada");
                locked = false; source.Readable = false; monitor.Poll();
                Check(activations == 2, "Un fallo de lectura no activa la simulación");
                source.Readable = true; settings.AutoActivate = false; reads = source.Reads; monitor.Poll();
                Check(activations == 2 && source.Reads == reads, "Desactivar la casilla detiene las comprobaciones de inactividad");
                settings = new AppSettings { IdleSeconds = 20 }; monitor.Poll();
                Check(activations == 2, "Los cambios de tiempo se aplican sin reiniciar la app");
                source.Sample = new IdleSample(12, 20000); monitor.Poll();
                Check(activations == 3, "Usa el nuevo tiempo configurado");
                monitor.Dispose(); active = false; reads = source.Reads; monitor.Poll();
                Check(activations == 3 && source.Reads == reads, "Cerrar la app detiene el monitor");
            }
            Console.WriteLine("Superadas " + checks + " comprobaciones de activación automática.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
