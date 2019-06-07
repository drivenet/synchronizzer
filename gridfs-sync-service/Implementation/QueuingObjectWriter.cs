using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal sealed class QueuingObjectWriter : IObjectWriter, IDisposable
    {
        private readonly QueuingTaskManager _tasks = new QueuingTaskManager();
        private readonly IObjectWriter _inner;

        public QueuingObjectWriter(IObjectWriter inner)
        {
            _inner = inner;
        }

        public Task Delete(string objectName, CancellationToken cancellationToken)
            => _tasks.Enqueue(
                token => _inner.Delete(objectName, token),
                cancellationToken);

        public void Dispose() => _tasks.Dispose();

        public async Task Flush(CancellationToken cancellationToken)
        {
            await _tasks.WaitAll(cancellationToken);
            await _inner.Flush(cancellationToken);
        }

        public Task Upload(string objectName, Stream readOnlyInput, CancellationToken cancellationToken)
            => _tasks.Enqueue(
                token => _inner.Upload(objectName, readOnlyInput, token),
                cancellationToken);
    }
}
