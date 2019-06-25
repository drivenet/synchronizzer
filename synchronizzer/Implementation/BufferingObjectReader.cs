using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class BufferingObjectReader : IObjectReader
    {
        private readonly IObjectReader _inner;

        public BufferingObjectReader(IObjectReader inner)
        {
            _inner = inner;
        }

        public async Task<Stream?> Read(string objectName, CancellationToken cancellationToken)
        {
            using var stream = await _inner.Read(objectName, cancellationToken);
            if (stream is object)
            {
                var length = checked((int)stream.Length);
                var bufferedStream = new MemoryStream(length);
                await stream.CopyToAsync(bufferedStream, cancellationToken);
                return bufferedStream;
            }

            return stream;
        }
    }
}
