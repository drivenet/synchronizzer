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

        public async Task<Stream?> Read(string objectName, CancellationToken cancellationToken)
        {
            using var stream = await _inner.Read(objectName, cancellationToken);
            if (stream is { Length: <= MaxBufferedLength })
            {
                var bufferedStream = new RecyclableMemoryStream(_streamManager, nameof(BufferingObjectReader), stream.Length);
                await stream.CopyToAsync(bufferedStream, cancellationToken);
                return bufferedStream;
            }

            return stream;
        }
    }
}
