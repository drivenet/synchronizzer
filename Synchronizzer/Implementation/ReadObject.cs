using System;
using System.IO;

namespace Synchronizzer.Implementation
{
    internal sealed class ReadObject : IDisposable
    {
        public ReadObject(Stream stream, long length)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            Length = length;
        }

        public Stream Stream { get; }

        public long Length { get; }

        public void Dispose() => Stream.Dispose();
    }
}
