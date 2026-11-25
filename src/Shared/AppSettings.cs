using System;
using System.IO;
using System.Xml.Serialization;

// Autor: Miquel Garrigo
public enum HumorMode { Discreto, Sarcasmo, Mascota, Todos }

public sealed class AppSettings
{
    public string DeviceName { get; set; }
    public int TimeoutSeconds { get; set; }
    public bool KeepAwake { get; set; }
    public bool ShowClock { get; set; }
    public string ImagePath { get; set; }
    public bool SimulateActivity { get; set; }
    public int ActivityIntervalSeconds { get; set; }
    public bool AutoActivate { get; set; }
    public int IdleSeconds { get; set; }
    public HumorMode Humor { get; set; }
    public bool WakeOnInput { get; set; }
    public int ActivateModifiers { get; set; }
    public int ActivateKey { get; set; }
    public bool SameHotkey { get; set; }
    public int UnlockModifiers { get; set; }
    public int UnlockKey { get; set; }
    internal static string FilePath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ajustes.xml"); } }

    public AppSettings()
    {
        DeviceName = "";
        TimeoutSeconds = 20;
        KeepAwake = true;
        ShowClock = true;
        ImagePath = "";
        SimulateActivity = true;
        ActivityIntervalSeconds = 300;
        AutoActivate = true;
        IdleSeconds = 300;
        Humor = HumorMode.Todos;
        WakeOnInput = true;
        ActivateModifiers = UnlockModifiers = 8;
        ActivateKey = UnlockKey = 0;
        SameHotkey = true;
    }

    internal static AppSettings Load()
    {
        try
        {
            using (var input = File.OpenRead(FilePath))
            {
                var settings = (AppSettings)new XmlSerializer(typeof(AppSettings)).Deserialize(input);
                if (settings.TimeoutSeconds < 0 || settings.TimeoutSeconds > 86400) settings.TimeoutSeconds = 20;
                if (settings.ActivityIntervalSeconds < 15 || settings.ActivityIntervalSeconds > 600) settings.ActivityIntervalSeconds = 300;
                if (settings.IdleSeconds < 1 || settings.IdleSeconds > 86400) settings.IdleSeconds = 300;
                if (!Enum.IsDefined(typeof(HumorMode), settings.Humor)) settings.Humor = HumorMode.Todos;
                settings.NormalizeShortcuts();
                return settings;
            }
        }
        catch { return new AppSettings(); }
    }

    internal void NormalizeShortcuts()
    {
        var defaults = new AppSettings();
        var activation = new HotkeySpec(ActivateModifiers & ~4, ActivateKey);
        var exit = new HotkeySpec(UnlockModifiers & ~4, UnlockKey);
        ActivateModifiers = activation.Error == null ? activation.Modifiers : defaults.ActivateModifiers;
        ActivateKey = activation.Error == null ? activation.Key : defaults.ActivateKey;
        UnlockModifiers = exit.Error == null ? exit.Modifiers : defaults.UnlockModifiers;
        UnlockKey = exit.Error == null ? exit.Key : defaults.UnlockKey;
    }

    internal void Save()
    {
        string temp = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var output = File.Create(temp)) new XmlSerializer(typeof(AppSettings)).Serialize(output, this);
            if (File.Exists(FilePath)) File.Replace(temp, FilePath, null);
            else File.Move(temp, FilePath);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    internal static byte[] DefaultXml()
    {
        using (var output = new MemoryStream())
        {
            new XmlSerializer(typeof(AppSettings)).Serialize(output, new AppSettings());
            return output.ToArray();
        }
    }
}
