using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class OriginReader : IOriginReader
    {
        private readonly IObjectSource _source;
        private readonly IObjectReader _reader;

        public OriginReader(IObjectSource source, IObjectReader reader)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        public Task<IReadOnlyCollection<ObjectInfo>> GetOrdered(string? fromName, CancellationToken cancellationToken) => _source.GetOrdered(fromName, cancellationToken);

        public Task<Stream?> Read(string objectName, CancellationToken cancellationToken) => _reader.Read(objectName, cancellationToken);
    }
}
