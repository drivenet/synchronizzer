using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal sealed class QueuingObjectManager : IObjectManager
    {
        private const double GoldenRatio = 1.618;

        private readonly HashSet<Task> _uploadQueue = new HashSet<Task>();
        private readonly HashSet<Task> _deleteQueue = new HashSet<Task>();
        private readonly IObjectManager _inner;

        public QueuingObjectManager(IObjectManager inner)
        {
            _inner = inner;
        }

        private static int MaxQueueSize => (int)Math.Ceiling(Environment.ProcessorCount * GoldenRatio);

        public async Task Delete(string objectName)
        {
            await EnsureQueueSize(_deleteQueue, MaxQueueSize);
            var task = _inner.Delete(objectName);
            _deleteQueue.Add(task);
        }

        public async Task Flush()
        {
            await Task.WhenAll(
                EnsureQueueSize(_uploadQueue, 0),
                EnsureQueueSize(_deleteQueue, 0));
            await _inner.Flush();
        }

        public async Task Upload(string objectName, Stream readOnlyInput)
        {
            await EnsureQueueSize(_uploadQueue, MaxQueueSize);
            var task = _inner.Upload(objectName, readOnlyInput);
            _uploadQueue.Add(task);
        }

        private static async Task EnsureQueueSize(HashSet<Task> queue, int size)
        {
            while (queue.Count >= size)
            {
                var task = await Task.WhenAny(queue);
                queue.Remove(task);
                await task;
            }
        }
    }
}
