using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal sealed class QueuingObjectWriter : IObjectWriter
    {
        private readonly IQueuingTaskManager _taskManager;
        private readonly IObjectWriter _inner;

        public QueuingObjectWriter(IObjectWriter inner, IQueuingTaskManager taskManager)
        {
            _inner = inner;
            _taskManager = taskManager;
        }

        public Task Delete(string objectName, CancellationToken cancellationToken)
            => _taskManager.Enqueue(
                token => _inner.Delete(objectName, token),
                cancellationToken);

        public async Task Flush(CancellationToken cancellationToken)
        {
            await _taskManager.WaitAll(cancellationToken);
            await _inner.Flush(cancellationToken);
        }

        public Task Upload(string objectName, Stream readOnlyInput, CancellationToken cancellationToken)
            => _taskManager.Enqueue(
                token => _inner.Upload(objectName, readOnlyInput, token),
                cancellationToken);
    }
}
