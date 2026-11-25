using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

// Autor: Miquel Garrigo
internal static class UpdateTransport
{
    internal static bool AllowedRedirect(Uri uri)
    {
        return uri != null && uri.IsAbsoluteUri && uri.Scheme == "https" && uri.IsDefaultPort &&
            string.IsNullOrEmpty(uri.UserInfo) && (uri.Host == "github.com" || uri.Host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase));
    }
    internal static async Task<HttpResponseMessage> OpenAsync(HttpClient client, Uri origin, CancellationToken cancellation)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                Uri uri = origin;
                for (int redirects = 0; ; redirects++)
                {
                    var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false);
                    int status = (int)response.StatusCode;
                    if (status == 301 || status == 302 || status == 303 || status == 307 || status == 308)
                    {
                        Uri next = response.Headers.Location;
                        response.Dispose();
                        if (next != null && !next.IsAbsoluteUri) next = new Uri(uri, next);
                        if (!AllowedRedirect(next) || redirects >= 5) throw new UpdateException("La redirección de GitHub no es válida. No se instalará ningún archivo.");
                        uri = next; continue;
                    }
                    if ((status == 408 || status == 429 || status >= 500) && attempt < 2)
                    { response.Dispose(); break; }
                    return response;
                }
            }
            catch (HttpRequestException) { if (attempt >= 2) throw; }
            catch (OperationCanceledException) { if (cancellation.IsCancellationRequested || attempt >= 2) throw; }
            await Task.Delay(500 * (attempt + 1), cancellation).ConfigureAwait(false);
        }
    }
}
