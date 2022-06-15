using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Ambermoon
{
    internal static class DownloadExtensions
    {
        public static async Task DownloadAsync(this HttpClient client, string requestUri, Stream destination,
            IProgress<float> progress, IProgress<long> progressInBytes, Action<long?> reportTotalSize,
            Action<System.Net.HttpStatusCode> reportStatusCode, CancellationToken cancellationToken = default)
        {
            // Get the http headers first to examine the content length
            using var response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            reportStatusCode?.Invoke(response.StatusCode);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                return;

            var contentLength = response.Content.Headers.ContentLength;
            reportTotalSize?.Invoke(contentLength);

            using var download = await response.Content.ReadAsStreamAsync(cancellationToken);

            // Ignore progress reporting when no progress reporter was 
            // passed or when the content length is unknown
            if (progress == null || !contentLength.HasValue)
            {
                await download.CopyToAsync(destination, cancellationToken);
                return;
            }

            // Convert absolute progress (bytes downloaded) into relative progress (0% - 100%)
            var relativeProgress = new Progress<long>(totalBytes =>
            {
                progressInBytes.Report(totalBytes);
                progress.Report((float)totalBytes / contentLength.Value);                
            });
            // Use extension method to report progress while downloading
            try
            {
                await download.CopyToAsync(destination, 81920, relativeProgress, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    progress.Report(-1); // report error
                    return;
                }
            }
            progress.Report(1);
        }

        public static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize,
            IProgress<long> progress = null, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (!source.CanRead)
                throw new ArgumentException("Has to be readable", nameof(source));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (!destination.CanWrite)
                throw new ArgumentException("Has to be writable", nameof(destination));
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            var buffer = new byte[bufferSize];
            long totalBytesRead = 0;
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                totalBytesRead += bytesRead;
                progress?.Report(totalBytesRead);

                if (cancellationToken.IsCancellationRequested)
                    return;
            }
        }
    }
}
