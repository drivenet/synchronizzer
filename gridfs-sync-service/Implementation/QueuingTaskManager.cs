using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal sealed class QueuingTaskManager : IDisposable, IQueuingTaskManager
    {
        private const double GoldenRatio = 1.618;

        private readonly ConcurrentDictionary<Task, CancellationTokenSource> _queue = new ConcurrentDictionary<Task, CancellationTokenSource>();
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        private static int MaxQueueSize => (int)Math.Ceiling(Environment.ProcessorCount * GoldenRatio);

        public void Dispose()
        {
            foreach (var cancel in _queue.Values)
            {
                cancel.Dispose();
            }

            _cancel.Dispose();
        }

        public async Task Enqueue(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            await EnsureQueueSize(MaxQueueSize - 1, cancellationToken);
            var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancel.Token, cancellationToken);
            try
            {
                var task = action(cts.Token);
                if (!_queue.TryAdd(task, cts))
                {
                    cts.Dispose();
                }
            }
            catch
            {
                try
                {
                    cts.Dispose();
                }
#pragma warning disable CA1031 // Do not catch general exception types -- critical recovery path
                catch
#pragma warning restore CA1031 // Do not catch general exception types
                {
                }

                throw;
            }
        }

        public Task WaitAll(CancellationToken cancellationToken)
            => EnsureQueueSize(0, cancellationToken);

        private Task EnsureQueueSize(int size, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_queue.Count <= size)
            {
                return Task.CompletedTask;
            }

            return EnsureQueueSizeSlow(size, cancellationToken);
        }

        private async Task EnsureQueueSizeSlow(int size, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using var registration = cancellationToken.Register(() => tcs.TrySetCanceled());
            do
            {
                var tasks = _queue.Keys.ToList();
                tasks.Add(tcs.Task);
                var task = await Task.WhenAny(tasks);
                if (_queue.TryRemove(task, out var cts))
                {
                    cts.Dispose();
                }

                try
                {
                    await task;
                }
                catch
                {
                    CancelAll();
                    throw;
                }
            }
            while (_queue.Count > size);
        }

        private void CancelAll()
        {
            var cancels = _queue.Values.ToList();
            foreach (var cancel in cancels)
            {
                try
                {
                    cancel.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }
    }
}
