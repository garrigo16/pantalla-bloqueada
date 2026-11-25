using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

// Autor: Miquel Garrigo
internal sealed class ShortcutEditor : StackPanel
{
    internal readonly CheckBox Win, Ctrl, Alt;
    internal readonly ComboBox Key;
    internal readonly TextBlock Heading;
    private readonly TextBlock preview;
    private bool updatingSelection;
    internal event Action Changed;
    internal ShortcutEditor(string title, HotkeySpec value)
    {
        Heading = new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 10) };
        Children.Add(Heading);
        var modifiers = new WrapPanel();
        Win = AddModifier(modifiers, "Win"); Ctrl = AddModifier(modifiers, "Ctrl");
        Alt = AddModifier(modifiers, "Alt");
        Children.Add(modifiers);
        var keys = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 7) };
        keys.Children.Add(new TextBlock { Text = "Tecla", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 14, 0) });
        Key = new ComboBox { Width = 110, MinHeight = 36, Padding = new Thickness(7, 4, 7, 4),
            Foreground = Brushes.Black, SelectedValuePath = "Tag" };
        keys.Children.Add(Key); Children.Add(keys);
        preview = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 14, Margin = new Thickness(0, 0, 0, 14) };
        Children.Add(preview);
        Key.SelectionChanged += delegate { Refresh(); };
        Spec = value;
    }
    private CheckBox AddModifier(Panel panel, string label)
    {
        var control = new CheckBox { Content = label, Margin = new Thickness(0, 0, 16, 9) };
        control.Checked += ModifiersChanged;
        control.Unchecked += ModifiersChanged;
        panel.Children.Add(control); return control;
    }
    private int Modifiers
    {
        get { return (Win.IsChecked == true ? 8 : 0) | (Ctrl.IsChecked == true ? 2 : 0) | (Alt.IsChecked == true ? 1 : 0); }
    }
    private void ModifiersChanged(object sender, RoutedEventArgs args)
    {
        if (updatingSelection) return;
        UpdateKeys(Spec.Key);
        Refresh();
    }
    private void UpdateKeys(int selectedKey)
    {
        Key.Items.Clear();
        AddKey(0);
        for (int key = 'A'; key <= 'Z'; key++) AddKey(key);
        for (int key = '0'; key <= '9'; key++) AddKey(key);
        for (int key = 0x70; key <= 0x87; key++) AddKey(key);
        Key.SelectedValue = selectedKey;
    }
    private void AddKey(int key)
    {
        if (!new HotkeySpec(Modifiers, key).WindowsUsesShortcut)
            Key.Items.Add(new ComboBoxItem { Content = HotkeySpec.KeyLabel(key), Tag = key });
    }
    internal HotkeySpec Spec
    {
        get
        {
            return new HotkeySpec(Modifiers, Key.SelectedValue is int ? (int)Key.SelectedValue : -1);
        }
        set
        {
            updatingSelection = true;
            try
            {
                Win.IsChecked = (value.Modifiers & 8) != 0;
                Ctrl.IsChecked = (value.Modifiers & 2) != 0;
                Alt.IsChecked = (value.Modifiers & 1) != 0;
                UpdateKeys(value.Key);
            }
            finally { updatingSelection = false; }
            Refresh();
        }
    }
    private void Refresh()
    {
        if (updatingSelection) return;
        var spec = Spec;
        string message = spec.Error;
        preview.Text = message ?? spec.ToString();
        preview.Foreground = message == null ? new SolidColorBrush(Color.FromRgb(166, 182, 209)) : Brushes.LightSalmon;
        if (Changed != null) Changed();
    }
}

internal sealed class ShortcutOptions : StackPanel
{
    internal readonly CheckBox Same;
    internal readonly ShortcutEditor Activate, Unlock;
    private readonly TextBlock error;
    private readonly TextBlock gamePreview;
    internal ShortcutOptions(AppSettings settings)
    {
        Same = new CheckBox { Content = "Usar el mismo atajo para activar y salir", IsChecked = settings.SameHotkey, Margin = new Thickness(0, 0, 0, 18) };
        Children.Add(Same);
        Activate = new ShortcutEditor("Activar", new HotkeySpec(settings.ActivateModifiers, settings.ActivateKey));
        Unlock = new ShortcutEditor("Salir de la simulación", new HotkeySpec(settings.UnlockModifiers, settings.UnlockKey));
        Children.Add(Activate); Children.Add(Unlock);
        gamePreview = new TextBlock { FontSize = 14, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10) };
        Children.Add(gamePreview);
        Children.Add(new TextBlock { Text = "Elige Ctrl, Alt o Win y una tecla disponible. La lista oculta los atajos de Windows. Para el pinball, añade Shift al atajo de salida.",
            TextWrapping = TextWrapping.Wrap, FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(166, 182, 209)), Margin = new Thickness(0, 0, 0, 12) });
        var reset = new Button { Content = "Restablecer atajos", HorizontalAlignment = HorizontalAlignment.Left, FontSize = 14 };
        reset.Click += delegate { Same.IsChecked = true; Activate.Spec = Unlock.Spec = new HotkeySpec(8, 0); };
        Children.Add(reset);
        error = new TextBlock { Foreground = Brushes.LightSalmon, TextWrapping = TextWrapping.Wrap, FontSize = 14, Margin = new Thickness(0, 12, 0, 0), Visibility = Visibility.Collapsed };
        Children.Add(error);
        Same.Checked += delegate { Refresh(); }; Same.Unchecked += delegate { Refresh(); };
        Activate.Changed += Refresh; Unlock.Changed += Refresh;
        Refresh();
    }
    private void Refresh()
    {
        Unlock.Visibility = Same.IsChecked == true ? Visibility.Collapsed : Visibility.Visible;
        Activate.Heading.Text = Same.IsChecked == true ? "Activar y salir de la simulación" : "Activar la simulación";
        var exit = Same.IsChecked == true ? Activate.Spec : Unlock.Spec;
        gamePreview.Text = exit.Error == null ? "Pinball: " + new HotkeySpec(exit.Modifiers | 4, exit.Key) : "";
        error.Text = ""; error.Visibility = Visibility.Collapsed;
    }
    internal void ApplyTo(AppSettings settings)
    {
        var active = Activate.Spec; var inactive = Unlock.Spec;
        string message = active.Error ?? (Same.IsChecked == true ? null : inactive.Error);
        if (message != null) throw new ShortcutException(message);
        settings.ActivateModifiers = active.Modifiers; settings.ActivateKey = active.Key;
        settings.SameHotkey = Same.IsChecked == true;
        settings.UnlockModifiers = inactive.Error == null ? inactive.Modifiers : active.Modifiers;
        settings.UnlockKey = inactive.Error == null ? inactive.Key : active.Key;
    }
    internal void ShowError(string message) { error.Text = message; error.Visibility = Visibility.Visible; }
}
