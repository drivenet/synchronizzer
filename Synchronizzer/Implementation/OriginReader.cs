using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class OriginReader : IOriginReader
    {
        private readonly IObjectSource _source;
        private readonly IObjectReader _reader;

        public OriginReader(IObjectSource source, IObjectReader reader, string address)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            Address = address ?? throw new ArgumentNullException(nameof(address));
        }

        public string Address { get; }

        public IAsyncEnumerable<IReadOnlyCollection<ObjectInfo>> GetOrdered(CancellationToken cancellationToken) => _source.GetOrdered(cancellationToken);

        public Task<ReadObject?> Read(string objectName, CancellationToken cancellationToken) => _reader.Read(objectName, cancellationToken);

        public override string ToString() => Address;
    }
}
