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

        public Task<IReadOnlyCollection<ObjectInfo>> GetOrdered(string? fromName, CancellationToken cancellationToken) => _source.GetOrdered(fromName, cancellationToken);

        public Task<ReadObject?> Read(string objectName, CancellationToken cancellationToken) => _reader.Read(objectName, cancellationToken);

        public override string ToString() => Address;
    }
}
