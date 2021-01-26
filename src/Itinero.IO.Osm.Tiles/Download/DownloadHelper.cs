using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using Itinero.Logging;

namespace Itinero.IO.Osm.Tiles.Download {
    internal static class DownloadHelper {
        /// <summary>
        /// Gets a stream for the content at the given url.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <returns>An open stream for the content at the given url.</returns>
        public static Stream Download(string url) {
            try {
                var client = new HttpClient();
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                var response = client.GetAsync(url);
                if (response.Result.StatusCode == HttpStatusCode.NotFound) {
                    return null;
                }

                var stream = response.GetAwaiter().GetResult().Content.ReadAsStreamAsync().GetAwaiter()
                    .GetResult();
                Logger.Log(nameof(DownloadHelper), TraceEventType.Verbose,
                    $"Downloaded from {url}.");
                return new GZipStream(stream, CompressionMode.Decompress);
            }
            catch (Exception ex) {
                Logger.Log(nameof(DownloadHelper), TraceEventType.Warning,
                    $"Failed to download from {url}: {ex}.");
                return null;
            }
        }
    }
}