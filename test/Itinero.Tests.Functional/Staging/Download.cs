using System.IO;
using System.Net;

namespace Itinero.Tests.Functional.Staging
{
    internal static class Download
    {
        public static string Get(string localFile, string url)
        {
            if (File.Exists(localFile)) {
                return localFile;
            }

            var client = new WebClient();
            client.DownloadFile(url, localFile);
            return localFile;
        }
    }
}