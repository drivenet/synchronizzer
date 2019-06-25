using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class RemoteWriter : IRemoteWriter
    {
        private readonly IObjectSource _source;
        private readonly IObjectWriter _writer;

        public RemoteWriter(IObjectSource source, IObjectWriter writer)
        {
            _source = source;
            _writer = writer;
        }

        public Task Delete(string objectName, CancellationToken cancellationToken) => _writer.Delete(objectName, cancellationToken);

        public Task Flush(CancellationToken cancellationToken) => _writer.Flush(cancellationToken);

        public Task<IReadOnlyCollection<ObjectInfo>> GetOrdered(string? fromName, CancellationToken cancellationToken) => _source.GetOrdered(fromName, cancellationToken);

        public Task Lock(CancellationToken cancellationToken) => _writer.Lock(cancellationToken);

        public Task Upload(string objectName, Stream readOnlyInput, CancellationToken cancellationToken) => _writer.Upload(objectName, readOnlyInput, cancellationToken);
    }
}
