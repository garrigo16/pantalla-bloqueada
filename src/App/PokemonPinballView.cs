using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

// Autor: Miquel Garrigo
internal static class PokemonPinballView
{
    private static readonly FontFamily Mono = new FontFamily("Consolas");
    private static readonly Brush Ink = ColorBrush(0x24363b);
    private static readonly Brush MutedInk = ColorBrush(0x455b56);
    private static readonly Brush Paper = ColorBrush(0xeaf0dd);
    private static readonly Brush Edge = ColorBrush(0x587369);
    private static readonly Brush LightText = ColorBrush(0xe1eedf);
    private static readonly Style ChoiceStyle = (Style)XamlReader.Parse(@"
<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Button'>
  <Setter Property='Foreground' Value='#24363B'/>
  <Setter Property='Cursor' Value='Hand'/>
  <Setter Property='Padding' Value='12,9'/>
  <Setter Property='HorizontalContentAlignment' Value='Stretch'/>
  <Setter Property='Template'>
    <Setter.Value>
      <ControlTemplate TargetType='Button'>
        <Grid Background='Transparent' SnapsToDevicePixels='True'>
          <Border Background='#5C746A' CornerRadius='3' Margin='0,3,0,0'/>
          <Border x:Name='face' Background='#FFFBEA' BorderBrush='#394C49' BorderThickness='2'
                  CornerRadius='3' Margin='0,0,0,3' Padding='{TemplateBinding Padding}'>
            <Grid>
              <Grid.ColumnDefinitions>
                <ColumnDefinition Width='20'/>
                <ColumnDefinition Width='*'/>
              </Grid.ColumnDefinitions>
              <Polygon x:Name='cursor' Points='0,0 9,5 0,10' Fill='#A94834' HorizontalAlignment='Left'
                       VerticalAlignment='Center' Opacity='0'/>
              <ContentPresenter Grid.Column='1' VerticalAlignment='Center'
                                HorizontalAlignment='{TemplateBinding HorizontalContentAlignment}' RecognizesAccessKey='True'/>
            </Grid>
          </Border>
        </Grid>
        <ControlTemplate.Triggers>
          <Trigger Property='IsMouseOver' Value='True'>
            <Setter TargetName='face' Property='Background' Value='#DAF0DA'/>
            <Setter TargetName='cursor' Property='Opacity' Value='1'/>
          </Trigger>
          <Trigger Property='Tag' Value='Selected'>
            <Setter TargetName='face' Property='Background' Value='#F7D98E'/>
            <Setter TargetName='cursor' Property='Opacity' Value='1'/>
          </Trigger>
          <Trigger Property='IsPressed' Value='True'>
            <Setter TargetName='face' Property='Background' Value='#E7BE68'/>
            <Setter TargetName='face' Property='Margin' Value='0,2,0,1'/>
          </Trigger>
        </ControlTemplate.Triggers>
      </ControlTemplate>
    </Setter.Value>
  </Setter>
</Style>");

    internal static Viewbox Chooser(Button original, Button ruby, Button back)
    {
        var menu = new StackPanel { Width = 444, UseLayoutRounding = true };
        menu.Children.Add(new Border
        {
            Background = Ink, Padding = new Thickness(19, 12, 19, 13),
            Child = new TextBlock { Text = "Elige el pinball", Foreground = LightText,
                FontSize = 24, FontWeight = FontWeights.Bold }
        });
        var options = new StackPanel { Background = Paper };
        var games = new StackPanel { Margin = new Thickness(16, 15, 16, 13) };
        original.Margin = new Thickness(0, 0, 0, 9);
        ruby.Margin = new Thickness(0, 0, 0, 12);
        back.HorizontalAlignment = HorizontalAlignment.Left;
        games.Children.Add(original);
        games.Children.Add(ruby);
        games.Children.Add(back);
        options.Children.Add(games);
        options.Children.Add(new Border
        {
            BorderBrush = Edge, BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 8, 16, 9),
            Child = new TextBlock { Text = "↑ ↓  Elegir     Enter  Jugar     Esc  Volver", FontFamily = Mono,
                TextAlignment = TextAlignment.Center, FontSize = 13, Foreground = MutedInk }
        });
        menu.Children.Add(options);

        return new Viewbox
        {
            StretchDirection = StretchDirection.DownOnly, Stretch = Stretch.Uniform,
            Margin = new Thickness(18), HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new Border { Background = Edge, BorderBrush = Edge,
                BorderThickness = new Thickness(3), CornerRadius = new CornerRadius(4), Child = menu }
        };
    }

    internal static Button Choice(string title, string detail = null)
    {
        var texts = new StackPanel();
        texts.Children.Add(new TextBlock { Text = title, FontSize = 17, FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap });
        if (detail != null)
            texts.Children.Add(new TextBlock { Text = detail, FontSize = 13, FontFamily = Mono, Foreground = MutedInk,
                Margin = new Thickness(0, 3, 0, 0) });

        var button = new Button { Content = texts, Style = ChoiceStyle, Tag = "" };
        AutomationProperties.SetName(button, title);
        return button;
    }

    internal static void SetSelected(Button button, bool selected)
    {
        button.Tag = selected ? "Selected" : "";
        AutomationProperties.SetItemStatus(button, selected ? "Seleccionado" : "");
    }

    internal static Border Controls()
    {
        var controls = new StackPanel();
        var shortcuts = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center };
        AddBinding(shortcuts, "Ctrl izq.", "Palanca izq.");
        AddBinding(shortcuts, "Ctrl der. / →", "Palanca der.");
        AddBinding(shortcuts, "Espacio", "Lanzar");
        AddBinding(shortcuts, "P", "Pausa / START");
        AddBinding(shortcuts, "Esc", "Salir");
        controls.Children.Add(shortcuts);
        controls.Children.Add(new TextBlock
        {
            Text = "Menús   ↑ ↓ ← D  Moverse   ·   Enter  Confirmar   ·   Retroceso  Volver",
            FontSize = 13, Foreground = MutedInk, TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center, Margin = new Thickness(4, 5, 4, 2)
        });
        var legend = new Border
        {
            Background = Paper, BorderBrush = Edge, BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(4),
            Margin = new Thickness(0, 10, 0, 0), Padding = new Thickness(6, 6, 6, 5), Child = controls,
            UseLayoutRounding = true
        };
        AutomationProperties.SetName(legend, "Controles de Pokémon Pinball");
        return legend;
    }

    private static void AddBinding(WrapPanel panel, string key, string action)
    {
        var binding = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 3, 4, 3) };
        binding.Children.Add(new Border
        {
            Background = Ink, CornerRadius = new CornerRadius(3), Padding = new Thickness(5, 2, 5, 2),
            Child = new TextBlock { Text = key, FontFamily = Mono, FontSize = 13, Foreground = LightText }
        });
        binding.Children.Add(new TextBlock { Text = action, Foreground = Ink,
            FontSize = 13, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0) });
        panel.Children.Add(binding);
    }

    private static Brush ColorBrush(int rgb)
    {
        var brush = new SolidColorBrush(Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb));
        brush.Freeze();
        return brush;
    }
}
