using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

// Autor: Miquel Garrigo
internal static class PinballDownload
{
    internal static string CheckedDirectory(string path)
    {
        Directory.CreateDirectory(path);
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("La caché del juego no puede ser un enlace.");
        return path;
    }

    internal static async Task<MemoryStream> FetchAsync(Uri url, string name, int bytes, CancellationToken cancellation)
    {
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        using (var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }))
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("DrifftAnalyzer/" + ProductVersion.Current);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            try
            {
                using (var response = await UpdateTransport.OpenAsync(client, url, timeout.Token).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        var zip = new MemoryStream(bytes);
                        try
                        {
                            var buffer = new byte[8192];
                            int count;
                            while ((count = await input.ReadAsync(buffer, 0, buffer.Length, timeout.Token).ConfigureAwait(false)) > 0)
                            {
                                if (zip.Length + count > bytes) throw new IOException("La descarga de " + name + " tiene un tamaño inesperado.");
                                zip.Write(buffer, 0, count);
                            }
                            zip.Position = 0;
                            return zip;
                        }
                        catch { zip.Dispose(); throw; }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                cancellation.ThrowIfCancellationRequested();
                throw new IOException("Se agotó el tiempo de descarga de " + name + ". Vuelve a intentarlo.");
            }
            catch (HttpRequestException ex)
            {
                throw new IOException("No se pudo descargar " + name + " de GitHub. Comprueba la conexión y vuelve a intentarlo.", ex);
            }
        }
    }
}
