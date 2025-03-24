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
        private readonly IDisposable? _disposable;

        public OriginReader(IObjectSource source, IObjectReader reader, IDisposable? disposable, string address)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            Address = address ?? throw new ArgumentNullException(nameof(address));
            _disposable = disposable;
        }

        public string Address { get; }

        public IAsyncEnumerable<IReadOnlyList<ObjectInfo>> GetOrdered(bool nice, CancellationToken cancellationToken) => _source.GetOrdered(nice, cancellationToken);

        public Task<ReadObject?> Read(string objectName, CancellationToken cancellationToken) => _reader.Read(objectName, cancellationToken);

        public override string ToString() => Address;

        public void Dispose() => _disposable?.Dispose();
    }
}
