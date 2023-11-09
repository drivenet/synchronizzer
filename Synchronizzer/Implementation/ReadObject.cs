using System;
using System.IO;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class ReadObject : IAsyncDisposable
    {
        public ReadObject(Stream stream, long length)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            Length = length;
        }

        public Stream Stream { get; }

        public long Length { get; }

        public ValueTask DisposeAsync() => Stream.DisposeAsync();
    }
}
