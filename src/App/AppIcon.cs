using System;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

// Autor: Miquel Garrigo
internal static class AppIcon
{
    // Recursos de toda la vida del proceso, compartidos por la bandeja y las ventanas.
    internal static readonly Drawing.Icon Tray = LoadTray();
    internal static readonly ImageSource Window = LoadWindow();

    private static Stream Open()
    {
        var input = Assembly.GetExecutingAssembly().GetManifestResourceStream("Bao.ico");
        if (input == null) throw new InvalidOperationException("Falta el icono de la aplicación.");
        return input;
    }
    private static Drawing.Icon LoadTray()
    {
        using (var input = Open())
        using (var icon = new Drawing.Icon(input, Forms.SystemInformation.SmallIconSize))
            return (Drawing.Icon)icon.Clone();
    }
    private static ImageSource LoadWindow()
    {
        using (var input = Open())
        {
            var decoder = BitmapDecoder.Create(input, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var image = decoder.Frames[decoder.Frames.Count - 1]; image.Freeze(); return image;
        }
    }
}
