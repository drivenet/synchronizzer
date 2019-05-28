using System;
using System.IO;
using System.Threading.Tasks;

using GridFSSyncService.Implementation;

namespace GridFSSyncService.Tests.Implementation
{
    internal sealed class ObjectReaderFake : IObjectReader
    {
        public Task<Stream> Read(string name)
        {
            var stream = new MemoryStream(Array.Empty<byte>(), false);
            return Task.FromResult<Stream>(stream);
        }
    }
}
