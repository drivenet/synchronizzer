using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal sealed class QueuingTaskManager : IDisposable, IQueuingTaskManager
    {
        private const double GoldenRatio = 1.618;

        private readonly HashSet<Task> _queue = new HashSet<Task>();
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        private static int MaxQueueSize => (int)Math.Ceiling(Environment.ProcessorCount * GoldenRatio);

        public void Dispose()
        {
            _cancel.Dispose();
        }

        public async Task Enqueue(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            await EnsureQueueSize(MaxQueueSize - 1, cancellationToken);
            var task = Run(action, cancellationToken);
            lock (_queue)
            {
                _queue.Add(task);
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
            var tasks = new List<Task>(_queue.Count);
            tasks.Add(tcs.Task);
            while (true)
            {
                lock (_queue)
                {
                    tasks.AddRange(_queue);
                }

                var task = await Task.WhenAny(tasks);
                bool removed;
                try
                {
                    await task;
                }
                finally
                {
                    lock (_queue)
                    {
                        removed = _queue.Remove(task);
                    }
                }

                if (!removed)
                {
                    throw new InvalidProgramException("Failed to remove task.");
                }

                if (_queue.Count <= size)
                {
                    break;
                }

                tasks.RemoveRange(1, tasks.Count - 1);
            }
        }

        private async Task Run(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancel.Token, cancellationToken);
            try
            {
                await action(cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                _cancel.Cancel();
                throw;
            }
        }
    }
}
