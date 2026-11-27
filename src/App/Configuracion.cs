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
using Microsoft.Win32;
using Forms = System.Windows.Forms;

// Autor: Miquel Garrigo
internal static class Startup
{
    private static string Command { get { return "\"" + Assembly.GetExecutingAssembly().Location + "\" --background"; } }

    internal static bool Enabled
    {
        get
        {
            using (var key = Registry.CurrentUser.OpenSubKey(StartupRegistration.KeyPath))
                return string.Equals(StartupRegistration.Read(key), Command, StringComparison.OrdinalIgnoreCase);
        }
        set
        {
            using (var key = Registry.CurrentUser.CreateSubKey(StartupRegistration.KeyPath))
            {
                if (value) StartupRegistration.Write(key, Command);
                else if (string.Equals(StartupRegistration.Read(key), Command, StringComparison.OrdinalIgnoreCase))
                    StartupRegistration.Write(key, null);
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
    private readonly CheckBox keepRunningWithLidClosed;
    private readonly CheckBox showClock;
    private readonly CheckBox startup;
    private readonly CheckBox simulateActivity;
    private readonly TextBox activityInterval;
    private readonly CheckBox autoActivate;
    private readonly TextBox idleSeconds;
    private readonly ComboBox humorMode;
    private readonly CheckBox wakeOnInput;
    private readonly ComboBox theme;
    private readonly TextBox lunarDelay;
    private readonly TextBlock timeoutHint, activityHint;
    private int titleClicks;
    private DateTime titleClicked;
    private readonly TextBlock imageLabel;
    private readonly TextBlock error;
    private readonly UpdateManager updates;
    private readonly TextBlock updateStatus;
    private readonly TextBlock lastCheck;
    private readonly Button searchUpdate;
    private readonly Button installUpdate;
    private readonly ShortcutOptions shortcutOptions;
    private readonly Expander shortcutExpander;
    private readonly Action<AppSettings, bool> savePreferences;
    private string imagePath, pokemonRomPath, pokemonRubyRomPath;

    internal SettingsWindow(AppSettings settings, UpdateManager updateManager = null, FunStats statistics = null, Action<AppSettings, bool> saveSettings = null, string lidPowerError = null)
    {
        updates = updateManager;
        savePreferences = saveSettings;
        Title = AppIdentity.DisplayName + " · Configuración";
        Icon = AppIcon.Window;
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
      <TextBlock x:Name='funHeading' Text='Tu pantalla, en pausa.' FontSize='27' FontWeight='SemiBold'/>
      <TextBlock Text='Ajusta la simulación a tu espacio de trabajo.' Foreground='#A6B6D1' Margin='0,8,0,27' TextWrapping='Wrap'/>
      <TextBlock Text='Pantalla con el fondo' Margin='0,0,0,9' FontWeight='SemiBold'/>
      <Grid><Grid.ColumnDefinitions><ColumnDefinition Width='*'/><ColumnDefinition Width='Auto'/></Grid.ColumnDefinitions>
        <ComboBox x:Name='displays' MinHeight='40' Padding='8,6' Foreground='#101728' VerticalContentAlignment='Center'/>
        <Button x:Name='identify' Grid.Column='1' Content='Identificar' Margin='10,0,0,0'/>
      </Grid>
      <TextBlock Text='Las demás pantallas permanecerán en negro.' Foreground='#A6B6D1' FontSize='14' Margin='0,8,0,23' TextWrapping='Wrap'/>
      <Expander x:Name='shortcutExpander' Header='Atajos de teclado' IsExpanded='False' Foreground='White' Margin='0,0,0,23'>
        <Border Background='#1A2740' CornerRadius='9' Padding='16' Margin='0,10,0,0'><ContentControl x:Name='shortcuts'/></Border>
      </Expander>
      <CheckBox x:Name='autoActivate' Content='Activar automáticamente por inactividad'/>
      <StackPanel Orientation='Horizontal' Margin='0,0,0,8'>
        <TextBlock Text='Después de' VerticalAlignment='Center'/><TextBox x:Name='idleSeconds' Width='92' MaxLength='5' Margin='12,0'/><TextBlock Text='segundos sin usar el PC' VerticalAlignment='Center'/>
      </StackPanel>
      <TextBlock Text='Cuenta teclado y ratón mientras la simulación está desactivada.' Foreground='#A6B6D1' FontSize='14' Margin='0,0,0,23' TextWrapping='Wrap'/>
      <TextBlock Text='Pasar la pantalla a negro después de' FontWeight='SemiBold' Margin='0,0,0,9'/>
      <StackPanel Orientation='Horizontal'>
        <TextBox x:Name='timeout' Width='92' MaxLength='5'/><TextBlock Text='segundos' Margin='12,0,0,0' VerticalAlignment='Center'/>
      </StackPanel>
      <TextBlock x:Name='timeoutHint' Text='0 mantiene el fondo visible sin límite de tiempo.' Foreground='#A6B6D1' FontSize='14' Margin='0,8,0,23' TextWrapping='Wrap'/>
      <TextBlock Text='Tema' FontWeight='SemiBold' Margin='0,0,0,9'/>
      <ComboBox x:Name='theme' AutomationProperties.Name='Tema' MinHeight='40' Padding='8,6' Foreground='#101728' Margin='0,0,0,20'>
        <ComboBoxItem Content='Clásico'/><ComboBoxItem Content='Pokémon'/>
      </ComboBox>
      <StackPanel x:Name='pokemonOptions' Margin='0,-9,0,23'>
        <StackPanel Orientation='Horizontal'>
          <TextBox x:Name='lunarDelay' AutomationProperties.Name='Espera en negro' Width='92' MaxLength='5'/>
          <TextBlock Text='segundos en negro antes del claro' Margin='12,0,0,0' VerticalAlignment='Center'/>
        </StackPanel>
        <TextBlock Text='Pokémon Pinball · Game Boy Color' FontWeight='SemiBold' Margin='0,12,0,3'/>
        <TextBlock x:Name='originalRomLabel' Foreground='#A6B6D1' Margin='0,0,0,8' TextTrimming='CharacterEllipsis'/>
        <WrapPanel>
          <Button x:Name='originalSelectRom' Content='Elegir ROM…' AutomationProperties.Name='Elegir ROM de Game Boy Color'/>
          <Button x:Name='originalResetRom' Content='Usar la europea' Margin='10,0,0,0'/>
        </WrapPanel>
        <TextBlock Text='Pokémon Pinball: Rubí y Zafiro · Game Boy Advance' FontWeight='SemiBold' TextWrapping='Wrap' Margin='0,12,0,3'/>
        <TextBlock x:Name='rubyRomLabel' Foreground='#A6B6D1' Margin='0,0,0,8' TextTrimming='CharacterEllipsis'/>
        <WrapPanel>
          <Button x:Name='rubySelectRom' Content='Elegir ROM…' AutomationProperties.Name='Elegir ROM de Rubí y Zafiro'/>
          <Button x:Name='rubyResetRom' Content='Usar la europea' Margin='10,0,0,0'/>
        </WrapPanel>
        <TextBlock Text='Por defecto se descarga la edición europea la primera vez que abres cada juego. Puedes elegir otra ROM. Selecciona Español dentro del juego.' Foreground='#A6B6D1' FontSize='14' Margin='0,8,0,0' TextWrapping='Wrap'/>
      </StackPanel>
      <TextBlock Text='Fondo' FontWeight='SemiBold' Margin='0,0,0,9'/>
      <TextBlock x:Name='imageLabel' Foreground='#A6B6D1' TextTrimming='CharacterEllipsis' Margin='0,0,0,10'/>
      <StackPanel Orientation='Horizontal' Margin='0,0,0,24'>
        <Button x:Name='image' Content='Elegir imagen…'/><Button x:Name='resetImage' Content='Predeterminado' Margin='10,0,0,0'/>
      </StackPanel>
      <CheckBox x:Name='showClock' Content='Mostrar reloj y fecha'/>
      <CheckBox x:Name='simulateActivity' Content='Mantener Windows desbloqueado'/>
      <TextBlock x:Name='activityHint' Text='Mueve el ratón un píxel alternando izquierda y derecha.' Foreground='#A6B6D1' FontSize='14' TextWrapping='Wrap' Margin='0,-5,0,12'/>
      <StackPanel Orientation='Horizontal' Margin='0,0,0,21'>
        <TextBlock Text='Actividad cada' VerticalAlignment='Center'/><TextBox x:Name='activityInterval' Width='76' MaxLength='3' Margin='12,0'/><TextBlock Text='segundos' VerticalAlignment='Center'/>
      </StackPanel>
      <CheckBox x:Name='keepAwake' Content='Evitar suspensión y apagado por inactividad'/>
      <TextBlock Text='Se aplica únicamente mientras la simulación esté activa.' Foreground='#A6B6D1' FontSize='14' TextWrapping='Wrap' Margin='0,-5,0,20'/>
      <CheckBox x:Name='keepRunningWithLidClosed' Content='Mantener el equipo activo al cerrar la tapa'/>
      <TextBlock x:Name='lidPowerHint' Text='Con batería o cargador, mientras la app siga abierta en la bandeja.' Foreground='#A6B6D1' FontSize='14' TextWrapping='Wrap' Margin='0,-5,0,20'/>
      <CheckBox x:Name='startup' Content='Iniciar con Windows'/>
      <Expander Header='Bromas y easter eggs' IsExpanded='True' Foreground='White' Margin='0,10,0,16'>
        <Border Background='#1A2740' CornerRadius='9' Padding='16' Margin='0,10,0,0'>
          <StackPanel>
            <TextBlock Text='Modo de humor' FontWeight='SemiBold' Margin='0,0,0,8'/>
            <ComboBox x:Name='humorMode' Foreground='#101728' Padding='8,6' MinHeight='40'>
              <ComboBoxItem Content='Discreto: sin bromas en pantalla'/><ComboBoxItem Content='Sarcasmo suave'/>
              <ComboBoxItem Content='Mascota y animaciones'/><ComboBoxItem Content='Todos los easter eggs'/>
            </ComboBox>
            <CheckBox x:Name='wakeOnInput' Content='Despertar el fondo al pulsar una tecla' Margin='0,16,0,7'/>
            <TextBlock Text='Lo muestra 4 segundos y vuelve a negro. Las bromas no tienen sonido.' Foreground='#A6B6D1' FontSize='14' TextWrapping='Wrap' Margin='0,0,0,15'/>
            <TextBlock x:Name='funStats' TextWrapping='Wrap' Margin='0,0,0,7'/>
            <TextBlock Text='Solo contadores de esta sesión; se reinician con la siguiente simulación.' Foreground='#A6B6D1' FontSize='14' TextWrapping='Wrap'/>
          </StackPanel>
        </Border>
      </Expander>
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
        shortcutExpander = (Expander)root.FindName("shortcutExpander");
        shortcutOptions = new ShortcutOptions(settings);
        ((ContentControl)root.FindName("shortcuts")).Content = shortcutOptions;
        displays = (ComboBox)root.FindName("displays");
        timeout = (TextBox)root.FindName("timeout");
        keepAwake = (CheckBox)root.FindName("keepAwake");
        keepRunningWithLidClosed = (CheckBox)root.FindName("keepRunningWithLidClosed");
        if (lidPowerError != null)
        {
            var hint = (TextBlock)root.FindName("lidPowerHint");
            hint.Text = "Windows no pudo aplicar o restaurar el ajuste. " + lidPowerError;
            hint.Foreground = new SolidColorBrush(Color.FromRgb(255, 172, 172));
        }
        showClock = (CheckBox)root.FindName("showClock");
        startup = (CheckBox)root.FindName("startup");
        simulateActivity = (CheckBox)root.FindName("simulateActivity");
        activityInterval = (TextBox)root.FindName("activityInterval");
        autoActivate = (CheckBox)root.FindName("autoActivate");
        idleSeconds = (TextBox)root.FindName("idleSeconds");
        humorMode = (ComboBox)root.FindName("humorMode");
        wakeOnInput = (CheckBox)root.FindName("wakeOnInput");
        theme = (ComboBox)root.FindName("theme");
        lunarDelay = (TextBox)root.FindName("lunarDelay");
        timeoutHint = (TextBlock)root.FindName("timeoutHint");
        activityHint = (TextBlock)root.FindName("activityHint");
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
        keepRunningWithLidClosed.IsChecked = settings.KeepRunningWithLidClosed;
        showClock.IsChecked = settings.ShowClock;
        startup.IsChecked = Startup.Enabled;
        simulateActivity.IsChecked = settings.SimulateActivity;
        activityInterval.Text = settings.ActivityIntervalSeconds.ToString(CultureInfo.InvariantCulture);
        activityInterval.IsEnabled = settings.SimulateActivity;
        autoActivate.IsChecked = settings.AutoActivate;
        idleSeconds.Text = settings.IdleSeconds.ToString(CultureInfo.InvariantCulture);
        idleSeconds.IsEnabled = settings.AutoActivate;
        humorMode.SelectedIndex = (int)settings.Humor;
        wakeOnInput.IsChecked = settings.WakeOnInput;
        theme.SelectedIndex = (int)settings.Theme;
        var pokemonOptions = (StackPanel)root.FindName("pokemonOptions");
        Action showPokemonOptions = delegate
        {
            pokemonOptions.Visibility = theme.SelectedIndex == (int)AppTheme.Pokemon ? Visibility.Visible : Visibility.Collapsed;
        };
        theme.SelectionChanged += delegate { showPokemonOptions(); };
        showPokemonOptions();
        lunarDelay.Text = settings.LunarDelaySeconds.ToString(CultureInfo.InvariantCulture);
        SetupRomPicker(root, "original", PokemonPinballTitle.Original, settings.PokemonRomPath,
            delegate(string path) { pokemonRomPath = path; });
        SetupRomPicker(root, "ruby", PokemonPinballTitle.RubySapphire, settings.PokemonRubyRomPath,
            delegate(string path) { pokemonRubyRomPath = path; });
        ((TextBlock)root.FindName("funStats")).Text = statistics == null ? "Los logros aparecerán después de una simulación." : statistics.Summary();
        autoActivate.Checked += delegate { idleSeconds.IsEnabled = true; };
        autoActivate.Unchecked += delegate { idleSeconds.IsEnabled = false; };
        simulateActivity.Checked += delegate { activityInterval.IsEnabled = true; };
        simulateActivity.Unchecked += delegate { activityInterval.IsEnabled = false; };
        timeout.TextChanged += delegate { RefreshFunHints(); };
        activityInterval.TextChanged += delegate { RefreshFunHints(); };
        humorMode.SelectionChanged += delegate { RefreshFunHints(); };
        ((TextBlock)root.FindName("funHeading")).MouseLeftButtonDown += delegate
        {
            if (humorMode.SelectedIndex == 0) return;
            var now = DateTime.UtcNow;
            titleClicks = now - titleClicked < TimeSpan.FromSeconds(5) ? titleClicks + 1 : 1;
            titleClicked = now;
            if (titleClicks >= 7) { ((TextBlock)root.FindName("funHeading")).Text = "Tu pantalla, en pausa. Tú, no."; titleClicks = 0; }
        };
        RefreshFunHints();
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

    internal void ShowShortcutError(string message)
    {
        shortcutExpander.IsExpanded = true;
        shortcutOptions.ShowError(message);
        shortcutExpander.BringIntoView();
    }

    private void RefreshFunHints()
    {
        timeoutHint.Text = "0 mantiene el fondo visible sin límite de tiempo.";
        activityHint.Text = "Mueve el ratón un píxel alternando izquierda y derecha.";
        if (humorMode.SelectedIndex == 0) return;
        if (timeout.Text == "42") timeoutHint.Text = "La respuesta. La pregunta, no la sabemos.";
        else if (timeout.Text == "86400") timeoutHint.Text = "24 horas. Eso ya es mudarse.";
        else if (timeout.Text == "0") timeoutHint.Text = "Fondo eterno. Valiente.";
        if (activityInterval.Text == "15") activityHint.Text = "Eso ya no es actividad, es un tic.";
    }

    private void SetupRomPicker(Grid root, string prefix, PokemonPinballTitle game, string current, Action<string> set)
    {
        var label = (TextBlock)root.FindName(prefix + "RomLabel");
        var reset = (Button)root.FindName(prefix + "ResetRom");
        Action<string> show = delegate(string path)
        {
            path = path ?? "";
            set(path);
            label.Text = path.Length == 0 ? "Edición europea · Descarga automática" : Path.GetFileName(path);
            label.ToolTip = path;
            reset.IsEnabled = path.Length != 0;
        };
        show(current);
        reset.Click += delegate { show(""); };
        ((Button)root.FindName(prefix + "SelectRom")).Click += delegate
        {
            string title = PokemonPinballRom.Title(game);
            var dialog = new OpenFileDialog
            {
                Title = "Elegir ROM de " + title,
                Filter = game == PokemonPinballTitle.Original ? "ROM de Game Boy|*.gbc;*.gb" : "ROM de Game Boy Advance|*.gba",
                CheckFileExists = true
            };
            if (dialog.ShowDialog(this) != true) return;
            try
            {
                PokemonPinballRom.Validate(dialog.FileName, game);
                show(dialog.FileName);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, title, MessageBoxButton.OK, MessageBoxImage.Warning); }
        };
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
        int idle;
        if (!int.TryParse(idleSeconds.Text, out idle) || idle < 1 || idle > 86400)
        {
            if (autoActivate.IsChecked == true)
            {
                error.Text = "El tiempo de inactividad debe estar entre 1 y 86400 segundos.";
                idleSeconds.Focus();
                return;
            }
            idle = 300;
        }
        int lunarSeconds;
        if (!int.TryParse(lunarDelay.Text, out lunarSeconds) || lunarSeconds < 0 || lunarSeconds > 86400)
        {
            if (theme.SelectedIndex == (int)AppTheme.Pokemon)
            {
                error.Text = "La espera en negro para la orilla debe estar entre 0 y 86400 segundos.";
                lunarDelay.BringIntoView();
                lunarDelay.Focus();
                return;
            }
            lunarSeconds = AppSettings.DefaultLunarDelay;
        }
        try
        {
            var settings = new AppSettings
            {
                DeviceName = selected.Screen.DeviceName, TimeoutSeconds = seconds,
                KeepAwake = keepAwake.IsChecked == true, ShowClock = showClock.IsChecked == true,
                KeepRunningWithLidClosed = keepRunningWithLidClosed.IsChecked == true,
                ImagePath = imagePath ?? "",
                SimulateActivity = simulateActivity.IsChecked == true, ActivityIntervalSeconds = interval,
                AutoActivate = autoActivate.IsChecked == true, IdleSeconds = idle,
                Humor = humorMode.SelectedIndex >= 0 ? (HumorMode)humorMode.SelectedIndex : HumorMode.Todos,
                WakeOnInput = wakeOnInput.IsChecked == true,
                Theme = theme.SelectedIndex == (int)AppTheme.Pokemon ? AppTheme.Pokemon : AppTheme.Clasico,
                LunarDelaySeconds = lunarSeconds, PokemonRomPath = pokemonRomPath, PokemonRubyRomPath = pokemonRubyRomPath
            };
            bool desiredStartup = startup.IsChecked == true;
            shortcutOptions.ApplyTo(settings);
            if (savePreferences != null) savePreferences(settings, desiredStartup);
            else
            {
                if (Startup.Enabled != desiredStartup) Startup.Enabled = desiredStartup;
                settings.Save();
            }
            if (Saved != null) Saved(settings);
            Close();
        }
        catch (ShortcutException ex) { ShowShortcutError(ex.Message); }
        catch (Exception ex) { error.Text = "No se pudo guardar. " + ex.Message; error.BringIntoView(); }
    }
}
