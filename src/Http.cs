using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ScrcpyManager
{
    // Shared HttpClient (replaces the obsolete WebClient/HttpWebRequest).
    internal static class Http
    {
        private static readonly HttpClient Client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };

        public static async Task<string> GetStringAsync(string url, string userAgent, int timeoutMs)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource(timeoutMs))
            using (HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, url))
            {
                req.Headers.TryAddWithoutValidation("User-Agent", userAgent);
                using (HttpResponseMessage resp = await Client.SendAsync(req, cts.Token).ConfigureAwait(false))
                {
                    resp.EnsureSuccessStatusCode();
                    return await resp.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
                }
            }
        }

        public static async Task DownloadFileAsync(string url, string path, string userAgent)
        {
            using (HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, url))
            {
                req.Headers.TryAddWithoutValidation("User-Agent", userAgent);
                using (HttpResponseMessage resp = await Client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    resp.EnsureSuccessStatusCode();
                    using (Stream source = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (FileStream target = File.Create(path))
                    {
                        await source.CopyToAsync(target).ConfigureAwait(false);
                    }
                }
            }
        }

        public static void DownloadFile(string url, string path, string userAgent)
        {
            Task.Run(() => DownloadFileAsync(url, path, userAgent)).GetAwaiter().GetResult();
        }
    }
}
