using DeviantartDownloader.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

namespace DeviantartDownloader.Extension {
    public static class HttpClientExtensions {
        public static async Task DownloadAsync(this HttpClient client, string requestUri, Stream destination, IProgress<string> speed, IProgress<float> progress = null, CancellationToken cancellationToken = default) {
            // Get the http headers first to examine the content length
            using (var response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead,cancellationToken)) {
                if(response.StatusCode == System.Net.HttpStatusCode.TooManyRequests) {
                    throw new RateLimitException();
                }
                var contentLength = response.Content.Headers.ContentLength;

                using (var download = await response.Content.ReadAsStreamAsync(cancellationToken)) {

                    // Ignore progress reporting when no progress reporter was 
                    // passed or when the content length is unknown
                    if (progress == null || !contentLength.HasValue) {
                        await download.CopyToAsync(destination);
                        return;
                    }

                    // Convert absolute progress (bytes downloaded) into relative progress (0% - 100%)
                    var relativeProgress = new Progress<float>(totalBytes => progress.Report((float)totalBytes / contentLength.Value));
                    // Use extension method to report progress while downloading
                    await download.CopyToAsync(destination, 81920, speed, relativeProgress, cancellationToken);
                    progress.Report(1);
                    speed.Report("");
                }
            }
        }
    }
}
