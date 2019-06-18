using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using GridFSSyncService.Implementation;

namespace GridFSSyncService.Tests.Implementation
{
    internal sealed class ObjectReaderMock : IObjectReader
    {
        private readonly Dictionary<string, Stream> _map = new Dictionary<string, Stream>();

        public Task<Stream> Read(string objectName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Stream stream = new MemoryStream(Array.Empty<byte>(), false);
            _map.Add(objectName, stream);
            return Task.FromResult(stream);
        }

        public Stream GetStream(string objectName) => _map[objectName];
    }
}
