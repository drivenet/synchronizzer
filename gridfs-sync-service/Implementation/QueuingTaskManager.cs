using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal sealed class QueuingTaskManager : IQueuingTaskManager
    {
        private const double GoldenRatio = 1.618;

        private readonly ConcurrentDictionary<Task, object> _queue = new ConcurrentDictionary<Task, object>();
        private readonly ConditionalWeakTable<object, CancellationTokenSource> _cancel = new ConditionalWeakTable<object, CancellationTokenSource>();

        private static int MaxQueueSize => (int)Math.Ceiling(Environment.ProcessorCount * GoldenRatio);

        public async Task Enqueue(object sender, Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            await EnsureQueueSize(MaxQueueSize - 1, null, cancellationToken);
            var task = Run(sender, action, cancellationToken);
            if (!_queue.TryAdd(task, sender))
            {
                throw new InvalidOperationException("The task was already added.");
            }
        }

        public Task WaitAll(object sender, CancellationToken cancellationToken)
            => EnsureQueueSize(0, sender, cancellationToken);

        private async Task EnsureQueueSize(int size, object? sender, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tcs = new TaskCompletionSource<bool>();
            using var registration = cancellationToken.Register(() => tcs.TrySetCanceled());
            var tasks = new List<Task>();
            while (true)
            {
                var queue = sender is object
                    ? _queue.Where(pair => pair.Value == sender).Select(pair => pair.Key)
                    : _queue.Keys;
                tasks.AddRange(queue);
                if (tasks.Count <= size)
                {
                    break;
                }

                tasks.Add(tcs.Task);
                var task = await Task.WhenAny(tasks);
                try
                {
                    await task;
                }
                finally
                {
                    _queue.TryRemove(task, out _);
                }

                tasks.Clear();
            }
        }

        private async Task Run(object sender, Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            var cancel = _cancel.GetOrCreateValue(sender);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel.Token, cancellationToken);
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
                cancel.Cancel();
                throw;
            }
        }
    }
}
