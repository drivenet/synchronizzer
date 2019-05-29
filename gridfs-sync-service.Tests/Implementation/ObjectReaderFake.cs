using System;
using System.IO;
using System.Threading.Tasks;

using GridFSSyncService.Implementation;

namespace GridFSSyncService.Tests.Implementation
{
    internal sealed class ObjectReaderFake : IObjectReader
    {
        private readonly Task<Stream> _emptyStream = Task.FromResult<Stream>(new MemoryStream(Array.Empty<byte>(), false));

        public Task<Stream> Read(string name) => _emptyStream;
    }
}
