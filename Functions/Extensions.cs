using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Zsnd_UI.Functions
{
    /// <summary>
    /// Other extensions
    /// </summary>
    public static class Extensions
    {
        public static void Sort<T>(this IList<T> collection, Func<IEnumerable<T>, IOrderedEnumerable<T>> sort)
        {
            T[] SC = [.. sort(collection)];
            for (int i = 0; i < SC.Length; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(SC[i], collection[i])) { collection[i] = SC[i]; }
                // Replace is most efficient. To improve UI animations and keep selection, do following:
                // int o = collection.IndexOf(SC[i]); if (o != i) { collection.Move(o, i); }
            }
        }
        /// <summary>
        /// Asynchronously reads the bytes from the <paramref name="source"/> stream and writes them to the <paramref name="destination"/> stream.
        /// Both stream positions are advanced by the number of bytes copied.
        /// Reports the <paramref name="progress"/> by counting the bytes copied, relative to the <paramref name="totalSize"/>.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents the asynchronous copy operation.</returns>
        public static async Task CopyToWithProgressAsync(
            this Stream source, FileStream destination,
            int totalSize, IProgress<double> progress,
            System.Threading.CancellationToken cancellationToken = default)
        {
            byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(0x14000); // to 0x40000 (i depends on buffer size)
            Memory<byte> mbuffer = buffer;
            try
            {
                int i = 0, prog = 0, bytesRead; while ((bytesRead = await source.ReadAsync(mbuffer, cancellationToken).ConfigureAwait(false)) != 0)
                {
                    await destination.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), cancellationToken).ConfigureAwait(false);
                    prog += bytesRead; i++;
                    if (i % 4 == 0 && prog < totalSize) { progress.Report(prog); }
                }
                progress.Report(totalSize);
            }
            finally { System.Buffers.ArrayPool<byte>.Shared.Return(buffer); }
        }
        /// <summary>
        /// Asynchronously reads sequence of bytes from the given file <paramref name="handle"/> at given <paramref name="offset"/>, and monitors cancellation requests.
        /// </summary>
        /// <param name="handle">The file handle.</param>
        /// <param name="buffer">A region of memory. When this method returns, the contents of this region are replaced by the bytes read from the file.</param>
        /// <param name="offset">The file position to read from.</param>
        /// <param name="minimumBytes">The number of bytes to be read from the file.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous read operation.</returns>
        public static async ValueTask ReadExactlyAsync(Microsoft.Win32.SafeHandles.SafeFileHandle handle,
            Memory<byte> buffer, long offset, int minimumBytes, System.Threading.CancellationToken cancellationToken)
        {
            int totalRead = 0;
            while (totalRead < minimumBytes)
            {
                int read = await RandomAccess.ReadAsync(handle, buffer[totalRead..], offset + totalRead, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new EndOfStreamException("Unable to read beyond the end of the file.");
                }
                totalRead += read;
            }
        }

        public static async ValueTask CopyToAsync(Microsoft.Win32.SafeHandles.SafeFileHandle handle,
            FileStream stream, long offset, int size, System.Threading.CancellationToken cancellationToken)
        {
            byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(0x14000);
            try
            {
                int bytesRead, bytesRemaining = size; while (bytesRemaining > 0
                    && (bytesRead = await RandomAccess.ReadAsync(handle,
                        new Memory<byte>(buffer, 0, Math.Min(0x14000, bytesRemaining)),
                        offset, cancellationToken).ConfigureAwait(false)) != 0)
                {
                    await stream.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), cancellationToken).ConfigureAwait(false);
                    bytesRemaining -= bytesRead;
                    offset += bytesRead;
                }
            }
            finally { System.Buffers.ArrayPool<byte>.Shared.Return(buffer); }
        }
    }
}
