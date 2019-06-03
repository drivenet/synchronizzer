using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal sealed class QueuingObjectWriter : IObjectWriter
    {
        private const double GoldenRatio = 1.618;

        private readonly HashSet<Task> _uploadQueue = new HashSet<Task>();
        private readonly HashSet<Task> _deleteQueue = new HashSet<Task>();
        private readonly IObjectWriter _inner;

        public QueuingObjectWriter(IObjectWriter inner)
        {
            _inner = inner;
        }

        private static int MaxQueueSize => (int)Math.Ceiling(Environment.ProcessorCount * GoldenRatio);

        public async Task Delete(string objectName, CancellationToken cancellationToken)
        {
            await EnsureQueueSize(_deleteQueue, MaxQueueSize, cancellationToken);
            var task = _inner.Delete(objectName, cancellationToken);
            _deleteQueue.Add(task);
        }

        public async Task Flush(CancellationToken cancellationToken)
        {
            await Task.WhenAll(
                EnsureQueueSize(_uploadQueue, 0, cancellationToken),
                EnsureQueueSize(_deleteQueue, 0, cancellationToken));
            await _inner.Flush(cancellationToken);
        }

        public async Task Upload(string objectName, Stream readOnlyInput, CancellationToken cancellationToken)
        {
            await EnsureQueueSize(_uploadQueue, MaxQueueSize, cancellationToken);
            var task = _inner.Upload(objectName, readOnlyInput, cancellationToken);
            _uploadQueue.Add(task);
        }

        private static async Task EnsureQueueSize(HashSet<Task> queue, int size, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using var registration = cancellationToken.Register(() => tcs.TrySetCanceled());
            while (queue.Count >= size)
            {
                var task = await Task.WhenAny(queue.Append(tcs.Task));
                queue.Remove(task);
                await task;
            }
        }
    }
}
