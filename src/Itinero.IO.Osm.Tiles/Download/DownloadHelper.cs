using System;
using System.IO;
using System.Net.Http;
using Itinero.Logging;

namespace Itinero.IO.Osm.Tiles.Download
{
    internal static class DownloadHelper
    {
        /// <summary>
        /// Gets a stream for the content at the given url.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <returns>An open stream for the content at the given url.</returns>
        public static Stream Download(string url)
        {
            try
            {
                var client = new HttpClient();
                var response = client.GetAsync(url);
                return response.GetAwaiter().GetResult().Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                Itinero.Logging.Logger.Log(nameof(DownloadHelper), TraceEventType.Warning, 
                    $"Failed to download from {url}.");
                return null;
            }
        }
    }
}