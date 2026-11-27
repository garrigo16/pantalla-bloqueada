using System;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;

internal static class LidPowerTests
{
    private sealed class FakePowerPlan : ILidPowerPlan
    {
        internal Guid Active = Guid.NewGuid();
        internal readonly Guid OriginalScheme;
        internal uint PluggedIn = 1, OnBattery = 2;
        internal bool DenyBatteryAccess, FailNextBatteryWrite, FailNextRefresh;
        internal int Writes, Refreshes;

        public FakePowerPlan() { OriginalScheme = Active; }

        public Guid ActiveScheme() { return Active; }

        public void CheckAccess(bool onBattery)
        {
            if (onBattery && DenyBatteryAccess) throw new Win32Exception(1260);
        }

        public uint Read(Guid scheme, bool onBattery)
        {
            if (scheme != OriginalScheme) throw new InvalidOperationException("Plan inesperado.");
            return onBattery ? OnBattery : PluggedIn;
        }

        public void Write(Guid scheme, bool onBattery, uint value)
        {
            Read(scheme, onBattery);
            if (onBattery && FailNextBatteryWrite)
            {
                FailNextBatteryWrite = false;
                throw new IOException("Fallo de escritura.");
            }
            if (onBattery) OnBattery = value;
            else PluggedIn = value;
            Writes++;
        }

        public void Refresh(Guid scheme)
        {
            if (FailNextRefresh)
            {
                FailNextRefresh = false;
                throw new IOException("Fallo al aplicar el plan.");
            }
            if (scheme == Active) Refreshes++;
        }
    }

    private sealed class ParentInput : MemoryStream
    {
        internal Action WhenRead;
        public override int ReadByte()
        {
            WhenRead();
            return -1;
        }
    }

    private static int checks;

    private static void Assert(bool value, string message)
    {
        if (!value) throw new Exception(message);
        checks++;
    }

    private static void Expect<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T) { checks++; return; }
        throw new Exception(message);
    }

    private static void CheckRestored(FakePowerPlan power, string backup)
    {
        Assert(power.PluggedIn == 1 && power.OnBattery == 2, "Deben restaurarse ambos valores.");
        Assert(!File.Exists(backup), "Debe limpiarse la copia tras restaurar.");
    }

    private static int Main()
    {
        string directory = Path.Combine(Path.GetTempPath(), "DrifftAnalyzer-LidTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string backup = Path.Combine(directory, "tapa.dat");
        try
        {
            var serializer = new XmlSerializer(typeof(AppSettings));
            using (var oldXml = new StringReader("<AppSettings><TimeoutSeconds>29</TimeoutSeconds></AppSettings>"))
            {
                var settings = (AppSettings)serializer.Deserialize(oldXml);
                Assert(!settings.KeepRunningWithLidClosed && settings.TimeoutSeconds == 29,
                    "El ajuste nuevo no debe activarse al migrar ni sustituir otros valores.");
            }
            using (var defaults = new MemoryStream(AppSettings.DefaultXml()))
                Assert(!((AppSettings)serializer.Deserialize(defaults)).KeepRunningWithLidClosed,
                    "Las instalaciones nuevas deben dejar el ajuste desactivado.");

            var power = new FakePowerPlan();
            var powerOverride = new LidPowerOverride(power, backup);
            using (var output = new StringWriter())
            using (var input = new ParentInput())
            {
                input.WhenRead = delegate
                {
                    Assert(output.ToString().Trim() == "OK", "El proceso debe confirmar al quedar listo.");
                    Assert(power.PluggedIn == 0 && power.OnBattery == 0, "Debe aplicar el ajuste en ambos modos.");
                    Assert(File.Exists(backup), "La copia tiene que sobrevivir al cierre forzado del proceso principal.");
                };
                LidPowerSession.RunSession(powerOverride, true, input, output);
            }
            CheckRestored(power, backup);
            Assert(power.Refreshes == 2, "Hay que aplicar el plan al activar y al restaurar.");

            power.DenyBatteryAccess = true;
            int writes = power.Writes;
            Expect<Win32Exception>(powerOverride.Apply, "El ajuste administrado por política debe fallar.");
            Assert(power.Writes == writes, "Una restricción de batería no permite escrituras parciales.");
            CheckRestored(power, backup);
            power.DenyBatteryAccess = false;

            power.FailNextBatteryWrite = true;
            Expect<IOException>(powerOverride.Apply, "Debe notificarse el fallo tras la primera escritura.");
            CheckRestored(power, backup);
            power.FailNextRefresh = true;
            Expect<IOException>(powerOverride.Apply, "Debe notificarse el error al aplicar el plan.");
            CheckRestored(power, backup);

            powerOverride.Apply();
            new LidPowerOverride(power, backup).Restore();
            CheckRestored(power, backup);

            powerOverride.Apply();
            power.PluggedIn = 3;
            power.Active = Guid.NewGuid();
            int refreshes = power.Refreshes;
            powerOverride.Restore();
            Assert(power.PluggedIn == 3 && power.OnBattery == 2, "Debe respetarse el valor cambiado durante la sesión.");
            Assert(power.Active != power.OriginalScheme && power.Refreshes == refreshes,
                "No debe seleccionarse el plan anterior si el usuario lo cambió.");
            Assert(!File.Exists(backup), "El plan anterior ya se restauró.");

            power = new FakePowerPlan { PluggedIn = 0, OnBattery = 0 };
            powerOverride = new LidPowerOverride(power, backup);
            powerOverride.Apply();
            powerOverride.Restore();
            Assert(power.Writes == 0 && power.Refreshes == 0 && !File.Exists(backup),
                "No se requieren cambios cuando ambos valores ya son cero.");

            power = new FakePowerPlan();
            powerOverride = new LidPowerOverride(power, backup);
            powerOverride.Apply();
            power.DenyBatteryAccess = true;
            Expect<Win32Exception>(powerOverride.Restore, "Debe informar si la política cambia antes de restaurar.");
            Assert(power.PluggedIn == 1 && power.OnBattery == 0 && File.Exists(backup),
                "Con restauración parcial, debe quedar pendiente el valor que no se pudo restaurar.");
            power.DenyBatteryAccess = false;
            using (var input = new MemoryStream())
            using (var output = new StringWriter())
                LidPowerSession.RunSession(powerOverride, false, input, output);
            CheckRestored(power, backup);
            Console.WriteLine("Tapa: {0} comprobaciones correctas.", checks);
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { Directory.Delete(directory, true); }
    }
}
