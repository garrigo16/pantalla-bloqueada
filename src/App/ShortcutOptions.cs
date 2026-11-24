using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

// Autor: Miquel Garrigo
internal sealed class ShortcutEditor : StackPanel
{
    internal readonly CheckBox Win, Ctrl, Alt, Shift;
    internal readonly ComboBox Key;
    internal readonly TextBlock Heading;
    private readonly TextBlock preview;
    internal event Action Changed;
    internal ShortcutEditor(string title, HotkeySpec value)
    {
        Heading = new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 10) };
        Children.Add(Heading);
        var modifiers = new WrapPanel();
        Win = AddModifier(modifiers, "Win"); Ctrl = AddModifier(modifiers, "Ctrl");
        Alt = AddModifier(modifiers, "Alt"); Shift = AddModifier(modifiers, "Shift");
        Children.Add(modifiers);
        var keys = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 7) };
        keys.Children.Add(new TextBlock { Text = "Tecla", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 14, 0) });
        Key = new ComboBox { Width = 110, MinHeight = 36, Padding = new Thickness(7, 4, 7, 4), Foreground = Brushes.Black };
        AddKey(0);
        for (int i = 0x41; i <= 0x5a; i++) AddKey(i);
        for (int i = 0x30; i <= 0x39; i++) AddKey(i);
        for (int i = 0x70; i <= 0x87; i++) if (i != 0x7b) AddKey(i);
        keys.Children.Add(Key); Children.Add(keys);
        preview = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 14, Margin = new Thickness(0, 0, 0, 14) };
        Children.Add(preview);
        Key.SelectionChanged += delegate { Refresh(); };
        Spec = value;
    }
    private CheckBox AddModifier(Panel panel, string label)
    {
        var control = new CheckBox { Content = label, Margin = new Thickness(0, 0, 16, 9) };
        control.Checked += delegate { Refresh(); }; control.Unchecked += delegate { Refresh(); };
        panel.Children.Add(control); return control;
    }
    private void AddKey(int key) { Key.Items.Add(new ComboBoxItem { Content = HotkeySpec.KeyLabel(key), Tag = key }); }
    internal HotkeySpec Spec
    {
        get
        {
            var item = Key.SelectedItem as ComboBoxItem;
            return new HotkeySpec((Win.IsChecked == true ? 8 : 0) | (Ctrl.IsChecked == true ? 2 : 0) |
                (Alt.IsChecked == true ? 1 : 0) | (Shift.IsChecked == true ? 4 : 0), item == null ? 0 : (int)item.Tag);
        }
        set
        {
            Win.IsChecked = (value.Modifiers & 8) != 0; Ctrl.IsChecked = (value.Modifiers & 2) != 0;
            Alt.IsChecked = (value.Modifiers & 1) != 0; Shift.IsChecked = (value.Modifiers & 4) != 0;
            foreach (ComboBoxItem item in Key.Items) if ((int)item.Tag == value.Key) { Key.SelectedItem = item; break; }
            Refresh();
        }
    }
    private void Refresh()
    {
        if (preview == null || Key == null) return;
        var spec = Spec;
        preview.Text = spec.Error ?? spec.ToString();
        preview.Foreground = spec.Error == null ? new SolidColorBrush(Color.FromRgb(166, 182, 209)) : Brushes.LightSalmon;
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
        Children.Add(new TextBlock { Text = "Elige Ctrl, Alt o Win y una tecla. Shift queda libre en la salida para abrir el pinball. Se aplican al guardar y solo se muestran aquí.",
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
        Activate.Shift.IsEnabled = Same.IsChecked != true;
        Unlock.Shift.IsEnabled = false;
        if (Same.IsChecked == true && Activate.Shift.IsChecked == true) Activate.Shift.IsChecked = false;
        if (Unlock.Shift.IsChecked == true) Unlock.Shift.IsChecked = false;
        Activate.Heading.Text = Same.IsChecked == true ? "Activar y salir de la simulación" : "Activar la simulación";
        var exit = Same.IsChecked == true ? Activate.Spec : Unlock.Spec;
        gamePreview.Text = "Pinball: " + new HotkeySpec(exit.Modifiers | 4, exit.Key);
        error.Text = ""; error.Visibility = Visibility.Collapsed;
    }
    internal void ApplyTo(AppSettings settings)
    {
        var active = Activate.Spec; var inactive = Unlock.Spec;
        string message = active.Error ?? (Same.IsChecked == true ? null : inactive.Error);
        if (message != null) throw new ShortcutException(message);
        settings.ActivateModifiers = active.Modifiers; settings.ActivateKey = active.Key;
        settings.SameHotkey = Same.IsChecked == true;
        settings.UnlockModifiers = inactive.Modifiers; settings.UnlockKey = inactive.Key;
    }
    internal void ShowError(string message) { error.Text = message; error.Visibility = Visibility.Visible; }
}
