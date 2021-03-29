using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.IO;

namespace Synchronizzer.Implementation
{
    internal sealed class BufferingObjectReader : IObjectReader
    {
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
            if (stream is object)
            {
                var length = checked((int)stream.Length);
                var bufferedStream = new RecyclableMemoryStream(_streamManager, nameof(BufferingObjectReader), length);
                await stream.CopyToAsync(bufferedStream, cancellationToken);
                return bufferedStream;
            }

            return stream;
        }
    }
}
