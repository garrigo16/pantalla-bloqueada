using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

// Autor: Miquel Garrigo
public sealed class AppSettings
{
    public string DeviceName { get; set; }
    public int TimeoutSeconds { get; set; }
    public bool KeepAwake { get; set; }
    public bool ShowClock { get; set; }
    public string ImagePath { get; set; }
    public bool SimulateActivity { get; set; }
    public int ActivityIntervalSeconds { get; set; }
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
                return settings;
            }
        }
        catch { return new AppSettings(); }
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
}

internal static class Startup
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private static string Command { get { return "\"" + Assembly.GetExecutingAssembly().Location + "\" --background"; } }

    internal static bool Enabled
    {
        get
        {
            using (var key = Registry.CurrentUser.OpenSubKey(KeyPath))
                return key != null && string.Equals(key.GetValue("PantallaBloqueada") as string, Command, StringComparison.OrdinalIgnoreCase);
        }
        set
        {
            using (var key = Registry.CurrentUser.CreateSubKey(KeyPath))
            {
                if (value) key.SetValue("PantallaBloqueada", Command, RegistryValueKind.String);
                else if (string.Equals(key.GetValue("PantallaBloqueada") as string, Command, StringComparison.OrdinalIgnoreCase))
                    key.DeleteValue("PantallaBloqueada", false);
            }
        }
    }
}

internal sealed class DisplayItem
{
    internal readonly Forms.Screen Screen;
    internal readonly int Number;
    internal DisplayItem(Forms.Screen screen, int number) { Screen = screen; Number = number; }
    public override string ToString()
    {
        return "Pantalla " + Number + (Screen.Primary ? " · Principal" : "") + " · " +
            Screen.Bounds.Width + " × " + Screen.Bounds.Height;
    }
}

internal sealed class SettingsWindow : Window
{
    internal event Action<AppSettings> Saved;
    private readonly ComboBox displays;
    private readonly TextBox timeout;
    private readonly CheckBox keepAwake;
    private readonly CheckBox showClock;
    private readonly CheckBox startup;
    private readonly CheckBox simulateActivity;
    private readonly TextBox activityInterval;
    private readonly TextBlock imageLabel;
    private readonly TextBlock error;
    private readonly UpdateManager updates;
    private readonly TextBlock updateStatus;
    private readonly TextBlock lastCheck;
    private readonly Button searchUpdate;
    private readonly Button installUpdate;
    private string imagePath;

    internal SettingsWindow(AppSettings settings, UpdateManager updateManager = null)
    {
        updates = updateManager;
        Title = "Pantalla bloqueada · Configuración";
        Width = 560;
        Height = 1050;
        MinWidth = 500;
        MinHeight = 440;
        MaxHeight = SystemParameters.WorkArea.Height;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromRgb(16, 23, 40));
        Foreground = Brushes.White;
        FontFamily = new FontFamily("Segoe UI");
        FontSize = 16;
        Content = XamlReader.Parse(@"
<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <Grid.Resources>
    <Style TargetType='Button'>
      <Setter Property='Background' Value='#26344F'/><Setter Property='Foreground' Value='White'/>
      <Setter Property='Padding' Value='16,9'/><Setter Property='BorderThickness' Value='0'/>
      <Setter Property='Cursor' Value='Hand'/><Setter Property='MinHeight' Value='40'/>
      <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='Button'>
        <Border x:Name='bg' Background='{TemplateBinding Background}' CornerRadius='7' Padding='{TemplateBinding Padding}'>
          <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
        </Border><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='bg' Property='Opacity' Value='0.8'/></Trigger><Trigger Property='IsEnabled' Value='False'><Setter TargetName='bg' Property='Opacity' Value='0.35'/></Trigger></ControlTemplate.Triggers>
      </ControlTemplate></Setter.Value></Setter>
    </Style>
    <Style TargetType='CheckBox'><Setter Property='Margin' Value='0,0,0,15'/><Setter Property='Foreground' Value='#E4EAF5'/><Setter Property='VerticalContentAlignment' Value='Center'/></Style>
    <Style TargetType='TextBox'><Setter Property='Padding' Value='10,8'/><Setter Property='Background' Value='#1A2740'/><Setter Property='Foreground' Value='White'/><Setter Property='BorderBrush' Value='#40516F'/><Setter Property='CaretBrush' Value='White'/></Style>
  </Grid.Resources>
  <Grid.RowDefinitions><RowDefinition Height='*'/><RowDefinition Height='Auto'/></Grid.RowDefinitions>
  <ScrollViewer VerticalScrollBarVisibility='Auto' HorizontalScrollBarVisibility='Disabled'>
    <StackPanel Margin='30,25,30,18'>
      <TextBlock Text='Tu pantalla, en pausa.' FontSize='27' FontWeight='SemiBold'/>
      <TextBlock Text='Ajusta la simulación a tu espacio de trabajo.' Foreground='#A6B6D1' Margin='0,8,0,27' TextWrapping='Wrap'/>
      <TextBlock Text='Pantalla con el fondo' Margin='0,0,0,9' FontWeight='SemiBold'/>
      <Grid><Grid.ColumnDefinitions><ColumnDefinition Width='*'/><ColumnDefinition Width='Auto'/></Grid.ColumnDefinitions>
        <ComboBox x:Name='displays' MinHeight='40' Padding='8,6' Foreground='#101728' VerticalContentAlignment='Center'/>
        <Button x:Name='identify' Grid.Column='1' Content='Identificar' Margin='10,0,0,0'/>
      </Grid>
      <TextBlock Text='Las demás pantallas permanecerán en negro.' Foreground='#A6B6D1' FontSize='14' Margin='0,8,0,23' TextWrapping='Wrap'/>
      <TextBlock Text='Pasar la pantalla a negro después de' FontWeight='SemiBold' Margin='0,0,0,9'/>
      <StackPanel Orientation='Horizontal'>
        <TextBox x:Name='timeout' Width='92' MaxLength='5'/><TextBlock Text='segundos' Margin='12,0,0,0' VerticalAlignment='Center'/>
      </StackPanel>
      <TextBlock Text='0 mantiene el fondo visible sin límite de tiempo.' Foreground='#A6B6D1' FontSize='14' Margin='0,8,0,23' TextWrapping='Wrap'/>
      <TextBlock Text='Fondo' FontWeight='SemiBold' Margin='0,0,0,9'/>
      <TextBlock x:Name='imageLabel' Foreground='#A6B6D1' TextTrimming='CharacterEllipsis' Margin='0,0,0,10'/>
      <StackPanel Orientation='Horizontal' Margin='0,0,0,24'>
        <Button x:Name='image' Content='Elegir imagen…'/><Button x:Name='resetImage' Content='Predeterminado' Margin='10,0,0,0'/>
      </StackPanel>
      <CheckBox x:Name='showClock' Content='Mostrar reloj y fecha'/>
      <CheckBox x:Name='simulateActivity' Content='Mantener Windows desbloqueado'/>
      <TextBlock Text='Mueve el ratón un píxel alternando izquierda y derecha.' Foreground='#A6B6D1' FontSize='14' TextWrapping='Wrap' Margin='0,-5,0,12'/>
      <StackPanel Orientation='Horizontal' Margin='0,0,0,21'>
        <TextBlock Text='Actividad cada' VerticalAlignment='Center'/><TextBox x:Name='activityInterval' Width='76' MaxLength='3' Margin='12,0'/><TextBlock Text='segundos' VerticalAlignment='Center'/>
      </StackPanel>
      <CheckBox x:Name='keepAwake' Content='Evitar suspensión y apagado por inactividad'/>
      <TextBlock Text='Se aplica únicamente mientras la simulación esté activa.' Foreground='#A6B6D1' FontSize='14' TextWrapping='Wrap' Margin='0,-5,0,20'/>
      <CheckBox x:Name='startup' Content='Iniciar con Windows'/>
      <Border Background='#1A2740' CornerRadius='9' Padding='16' Margin='0,10,0,16'>
        <StackPanel>
          <TextBlock Text='Actualizaciones' FontWeight='SemiBold' Margin='0,0,0,7'/>
          <TextBlock x:Name='installedVersion' Foreground='#A6B6D1' FontSize='14'/>
          <TextBlock x:Name='updateStatus' TextWrapping='Wrap' Margin='0,8,0,5'/>
          <TextBlock x:Name='lastCheck' Foreground='#A6B6D1' FontSize='14' TextWrapping='Wrap' Margin='0,0,0,12'/>
          <WrapPanel>
            <Button x:Name='searchUpdate' Content='Buscar actualizaciones' Margin='0,0,8,8' Padding='12,9' FontSize='14'/>
            <Button x:Name='installUpdate' Content='Actualizar' IsEnabled='False' Background='#65A9FF' Foreground='#0D1830' FontWeight='SemiBold' Margin='0,0,0,8' Padding='12,9' FontSize='14'/>
          </WrapPanel>
        </StackPanel>
      </Border>
      <TextBlock x:Name='error' Foreground='#FFACAC' TextWrapping='Wrap' FontSize='14'/>
    </StackPanel>
  </ScrollViewer>
  <Border Grid.Row='1' Background='#141E33' Padding='30,16'>
    <StackPanel Orientation='Horizontal' HorizontalAlignment='Right'>
      <Button x:Name='cancel' Content='Cancelar' Margin='0,0,10,0'/>
      <Button x:Name='save' Content='Guardar' Background='#65A9FF' Foreground='#0D1830' FontWeight='SemiBold'/>
    </StackPanel>
  </Border>
</Grid>");
        var root = (Grid)Content;
        displays = (ComboBox)root.FindName("displays");
        timeout = (TextBox)root.FindName("timeout");
        keepAwake = (CheckBox)root.FindName("keepAwake");
        showClock = (CheckBox)root.FindName("showClock");
        startup = (CheckBox)root.FindName("startup");
        simulateActivity = (CheckBox)root.FindName("simulateActivity");
        activityInterval = (TextBox)root.FindName("activityInterval");
        imageLabel = (TextBlock)root.FindName("imageLabel");
        error = (TextBlock)root.FindName("error");
        updateStatus = (TextBlock)root.FindName("updateStatus");
        lastCheck = (TextBlock)root.FindName("lastCheck");
        searchUpdate = (Button)root.FindName("searchUpdate");
        installUpdate = (Button)root.FindName("installUpdate");
        ((TextBlock)root.FindName("installedVersion")).Text = "Versión instalada: " + ProductVersion.Current;
        imagePath = settings.ImagePath;
        RefreshScreens(settings.DeviceName);
        timeout.Text = settings.TimeoutSeconds.ToString(CultureInfo.InvariantCulture);
        keepAwake.IsChecked = settings.KeepAwake;
        showClock.IsChecked = settings.ShowClock;
        startup.IsChecked = Startup.Enabled;
        simulateActivity.IsChecked = settings.SimulateActivity;
        activityInterval.Text = settings.ActivityIntervalSeconds.ToString(CultureInfo.InvariantCulture);
        activityInterval.IsEnabled = settings.SimulateActivity;
        simulateActivity.Checked += delegate { activityInterval.IsEnabled = true; };
        simulateActivity.Unchecked += delegate { activityInterval.IsEnabled = false; };
        UpdateImageLabel();
        ((Button)root.FindName("identify")).Click += delegate { Identify(); };
        ((Button)root.FindName("image")).Click += delegate
        {
            var dialog = new OpenFileDialog { Title = "Elegir fondo", Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp", CheckFileExists = true };
            if (dialog.ShowDialog(this) == true) { imagePath = dialog.FileName; UpdateImageLabel(); }
        };
        ((Button)root.FindName("resetImage")).Click += delegate { imagePath = ""; UpdateImageLabel(); };
        ((Button)root.FindName("cancel")).Click += delegate { Close(); };
        ((Button)root.FindName("save")).Click += delegate { Save(); };
        searchUpdate.Click += async delegate { if (updates != null) await updates.CheckAsync(); };
        installUpdate.Click += async delegate { if (updates != null) await updates.InstallAsync(); };
        if (updates != null) updates.Changed += RefreshUpdates;
        Closed += delegate { if (updates != null) updates.Changed -= RefreshUpdates; };
        RefreshUpdates(this, EventArgs.Empty);
    }

    private void RefreshUpdates(object sender, EventArgs e)
    {
        searchUpdate.IsEnabled = updates != null && !updates.IsBusy;
        installUpdate.IsEnabled = updates != null && !updates.IsBusy && updates.Available != null;
        installUpdate.Content = updates != null && updates.Available != null ? "Actualizar a " + updates.Available.Version : "Actualizar";
        updateStatus.Text = updates == null ? "No hay servicio de actualizaciones en esta vista." : updates.Message;
        updateStatus.Foreground = updates != null && updates.Available != null
            ? new SolidColorBrush(Color.FromRgb(255, 207, 110)) : Brushes.White;
        lastCheck.Text = updates == null || updates.LastCheckUtc == DateTime.MinValue
            ? "Comprobación automática cada 12 horas."
            : "Última comprobación: " + updates.LastCheckUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("es-ES"));
    }

    private void UpdateImageLabel()
    {
        imageLabel.Text = string.IsNullOrEmpty(imagePath) ? "Fondo predeterminado de Windows" : Path.GetFileName(imagePath);
    }

    private void RefreshScreens(string selected)
    {
        displays.Items.Clear();
        int number = 0;
        foreach (var screen in Forms.Screen.AllScreens)
        {
            var item = new DisplayItem(screen, ++number);
            displays.Items.Add(item);
            if (screen.DeviceName == selected || (string.IsNullOrEmpty(selected) && screen.Primary)) displays.SelectedItem = item;
        }
        if (displays.SelectedIndex < 0 && displays.Items.Count > 0)
        {
            foreach (DisplayItem item in displays.Items) if (item.Screen.Primary) displays.SelectedItem = item;
        }
    }

    private void Identify()
    {
        var selected = displays.SelectedItem as DisplayItem;
        RefreshScreens(selected == null ? "" : selected.Screen.DeviceName);
        foreach (DisplayItem item in displays.Items)
        {
            var badge = new Window
            {
                Width = 180, Height = 140, WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize,
                Topmost = true, ShowInTaskbar = false, Background = new SolidColorBrush(Color.FromRgb(25, 40, 65)),
                Content = new TextBlock { Text = item.Number.ToString(), Foreground = Brushes.White,
                    FontSize = 76, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center }
            };
            var bounds = item.Screen.Bounds;
            badge.SourceInitialized += delegate
            {
                Native.SetWindowPos(new WindowInteropHelper(badge).Handle, new IntPtr(-1),
                    bounds.X + (bounds.Width - 180) / 2, bounds.Y + (bounds.Height - 140) / 2, 180, 140, 0x0010);
            };
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            timer.Tick += delegate { timer.Stop(); badge.Close(); };
            badge.Show();
            timer.Start();
        }
    }

    private void Save()
    {
        int seconds;
        if (!int.TryParse(timeout.Text, out seconds) || seconds < 0 || seconds > 86400)
        {
            error.Text = "Escribe un número de segundos entre 0 y 86400.";
            timeout.Focus();
            return;
        }
        var selected = displays.SelectedItem as DisplayItem;
        if (selected == null) { error.Text = "Selecciona una pantalla."; return; }
        int interval;
        if (!int.TryParse(activityInterval.Text, out interval) || interval < 15 || interval > 600)
        {
            error.Text = "El intervalo de actividad debe estar entre 15 y 600 segundos.";
            activityInterval.Focus();
            return;
        }
        try
        {
            var settings = new AppSettings
            {
                DeviceName = selected.Screen.DeviceName, TimeoutSeconds = seconds,
                KeepAwake = keepAwake.IsChecked == true, ShowClock = showClock.IsChecked == true,
                ImagePath = imagePath ?? "",
                SimulateActivity = simulateActivity.IsChecked == true, ActivityIntervalSeconds = interval
            };
            bool desiredStartup = startup.IsChecked == true;
            if (Startup.Enabled != desiredStartup) Startup.Enabled = desiredStartup;
            settings.Save();
            if (Saved != null) Saved(settings);
            Close();
        }
        catch (Exception ex) { error.Text = "No se pudo guardar. " + ex.Message; }
    }
}
