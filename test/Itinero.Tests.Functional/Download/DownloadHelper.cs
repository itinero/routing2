using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using Itinero.Logging;

namespace Itinero.Tests.Functional.Download
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
            var fileName = HttpUtility.UrlEncode(url) + ".tile.zip";
            fileName = Path.Combine(".", "cache", fileName);

            if (File.Exists(fileName))
            {
                return new GZipStream(File.OpenRead(fileName), CompressionMode.Decompress);
            }
                
            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                var response = client.GetAsync(url);
                if (response.Result.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
                using (var stream = response.GetAwaiter().GetResult().Content.ReadAsStreamAsync().GetAwaiter()
                    .GetResult()) 
                using (var fileStream = File.Open(fileName, FileMode.Create))
                {
                    stream.CopyTo(fileStream);    
                }
                
                Itinero.Logging.Logger.Log(nameof(DownloadHelper), TraceEventType.Verbose, 
                    $"Downloaded from {url}.");
            }
            catch (Exception ex)
            {
                Itinero.Logging.Logger.Log(nameof(DownloadHelper), TraceEventType.Warning, 
                    $"Failed to download from {url}: {ex}.");
                return null;
            }
            
            return new GZipStream(File.OpenRead(fileName), CompressionMode.Decompress);
        }
    }
}