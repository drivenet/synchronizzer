using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal sealed class LocalReader : ILocalReader
    {
        private readonly IObjectSource _source;
        private readonly IObjectReader _reader;

        public LocalReader(IObjectSource source, IObjectReader reader)
        {
            _source = source;
            _reader = reader;
        }

        public Task<IEnumerable<ObjectInfo>> GetOrdered(string? fromName, CancellationToken cancellationToken) => _source.GetOrdered(fromName, cancellationToken);

        public Task<Stream> Read(string objectName, CancellationToken cancellationToken) => _reader.Read(objectName, cancellationToken);
    }
}
