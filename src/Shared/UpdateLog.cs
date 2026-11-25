using System;
using System.Diagnostics;
using System.IO;
using System.Text;

// Autor: Miquel Garrigo
internal static class UpdateLog
{
    private static readonly object gate = new object();
    internal static string DirectoryPath = AppDomain.CurrentDomain.BaseDirectory;
    internal static void Write(string operation, Exception error = null)
    {
        try
        {
            lock (gate)
            {
                string path = Path.Combine(DirectoryPath, "actualizador.log");
                if (File.Exists(path) && new FileInfo(path).Length > 256 * 1024) File.Delete(path);
                var line = new StringBuilder(DateTime.UtcNow.ToString("o") + " " + ProductVersion.Current + " " + operation);
                if (error != null)
                {
                    Exception detail = error.GetBaseException();
                    line.Append(" ").Append(detail.GetType().FullName).Append(" 0x").Append(detail.HResult.ToString("X8"));
                    var frames = new StackTrace(detail, false).GetFrames();
                    if (frames != null) foreach (var frame in frames)
                    {
                        var method = frame.GetMethod();
                        if (method != null && method.DeclaringType != null && method.DeclaringType.Assembly == typeof(UpdateLog).Assembly)
                            line.Append(" ").Append(method.DeclaringType.Name).Append(".").Append(method.Name);
                    }
                }
                // No incluir mensajes de excepción, rutas, URLs, ajustes ni datos personales.
                File.AppendAllText(path, line.AppendLine().ToString(), new UTF8Encoding(false));
            }
        }
        catch { /* Un diagnóstico no puede impedir usar o actualizar la app. */ }
    }
}
