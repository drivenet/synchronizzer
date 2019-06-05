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
            await EnsureQueueSize(_deleteQueue, MaxQueueSize - 1, cancellationToken);
            var task = _inner.Delete(objectName, cancellationToken);
            lock (_deleteQueue)
            {
                _deleteQueue.Add(task);
            }
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
            await EnsureQueueSize(_uploadQueue, MaxQueueSize - 1, cancellationToken);
            var task = _inner.Upload(objectName, readOnlyInput, cancellationToken);
            lock (_uploadQueue)
            {
                _uploadQueue.Add(task);
            }
        }

        private static Task EnsureQueueSize(HashSet<Task> queue, int size, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (queue.Count <= size)
            {
                return Task.CompletedTask;
            }

            return EnsureQueueSizeSlow(queue, size, cancellationToken);
        }

        private static async Task EnsureQueueSizeSlow(HashSet<Task> queue, int size, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using var registration = cancellationToken.Register(() => tcs.TrySetCanceled());
            do
            {
                List<Task> tasks;
                lock (queue)
                {
                    tasks = queue.ToList();
                }

                tasks.Add(tcs.Task);
                var task = await Task.WhenAny(tasks);
                bool removed;
                lock (queue)
                {
                    removed = queue.Remove(task);
                }

                if (!removed)
                {
                    throw new InvalidDataException(FormattableString.Invariant($"Missing task {task} in queue."));
                }

                await task;
            }
            while (queue.Count > size);
        }
    }
}
