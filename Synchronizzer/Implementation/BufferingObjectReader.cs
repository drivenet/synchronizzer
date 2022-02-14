using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.IO;

namespace Synchronizzer.Implementation
{
    internal sealed class BufferingObjectReader : IObjectReader
    {
        private const long MaxBufferedLength = 16 << 20;

        private readonly IObjectReader _inner;
        private readonly RecyclableMemoryStreamManager _streamManager;

        public BufferingObjectReader(IObjectReader inner, RecyclableMemoryStreamManager streamManager)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _streamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        }

        public async Task<ReadObject?> Read(string objectName, CancellationToken cancellationToken)
        {
            var readObject = await _inner.Read(objectName, cancellationToken);
            if (readObject is { Length: <= MaxBufferedLength })
            {
                Stream bufferedStream;
                try
                {
#pragma warning disable CA2000 // Dispose objects before losing scope -- passed to ReadObject for future disposal
                    bufferedStream = new RecyclableMemoryStream(_streamManager, nameof(BufferingObjectReader), readObject.Length);
#pragma warning restore CA2000 // Dispose objects before losing scope
                    await readObject.Stream.CopyToAsync(bufferedStream, cancellationToken);
                }
                finally
                {
                    readObject.Dispose();
                }

                return new ReadObject(bufferedStream, bufferedStream.Length);
            }

            return readObject;
        }
    }
}
