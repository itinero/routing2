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
        public static Stream? Download(string url)
        {
            
            var fileName = HttpUtility.UrlEncode(url) + ".tile.zip";
            fileName = Path.Combine(".", "cache", fileName);

            if (File.Exists(fileName))
            {
                return new GZipStream(File.OpenRead(fileName), CompressionMode.Decompress);
            }
            
            var redirectFileName = HttpUtility.UrlEncode(url) + ".tile.redirect";
            redirectFileName = Path.Combine(".", "cache", redirectFileName);

            if (File.Exists(redirectFileName))
            {
                var newUrl = File.ReadAllText(redirectFileName);
                return Download(newUrl);
            }
                
            try
            {
                var handler = new HttpClientHandler {AllowAutoRedirect = false};

                var client = new HttpClient(handler);
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                var response = client.GetAsync(url);
                switch (response.Result.StatusCode)
                {
                    case HttpStatusCode.NotFound:
                        return null;
                    case HttpStatusCode.Moved:
                    {
                        return Download(response.Result.Headers.Location.ToString());
                    }
                    case HttpStatusCode.Redirect:
                    {
                        var uri = new Uri(url);
                        var redirected = new Uri($"{uri.Scheme}://{uri.Host}{response.Result.Headers.Location}");

                        using var stream = File.Open(redirectFileName, FileMode.Create);
                        using var streamWriter = new StreamWriter(stream);
                        streamWriter.Write(redirected);
                        
                        return Download(redirected.ToString());
                    }
                }

                var temp = $"{Guid.NewGuid()}.temp";
                using (var stream = response.GetAwaiter().GetResult().Content.ReadAsStreamAsync().GetAwaiter()
                    .GetResult())
                using (var fileStream = File.Open(temp, FileMode.Create))
                {
                    stream.CopyTo(fileStream);    
                }
                
                if (File.Exists(fileName)) File.Delete(fileName);
                File.Move(temp, fileName);
                
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