using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class QueuingTaskManager : IQueuingTaskManager
    {
        private const double GoldenRatio = 1.618;

        private readonly ConcurrentDictionary<Task, object> _queue = new ConcurrentDictionary<Task, object>();
        private readonly ConditionalWeakTable<object, CancellationTokenSource> _cancel = new ConditionalWeakTable<object, CancellationTokenSource>();

        private static int MaxQueueSize => (int)Math.Ceiling(Environment.ProcessorCount / GoldenRatio);

        public async Task Enqueue(object sender, Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            await EnsureQueueSize(MaxQueueSize - 1, cancellationToken);
            var task = Run(sender, action, cancellationToken);
            if (!_queue.TryAdd(task, sender))
            {
                throw new InvalidOperationException("The task was already added.");
            }
        }

        public async Task WaitAll(object sender)
        {
            var tasks = new List<Task>();
            foreach (var pair in _queue)
            {
                if (pair.Value == sender
                    && _queue.TryRemove(pair.Key, out _))
                {
                    tasks.Add(pair.Key);
                }
            }

            await Task.WhenAll(tasks);
        }

        private async Task EnsureQueueSize(int size, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tcs = new TaskCompletionSource<bool>();
            using var registration = cancellationToken.Register(() => tcs.TrySetCanceled());
            var tasks = new List<Task>();
            while (true)
            {
                foreach (var pair in _queue)
                {
                    tasks.Add(pair.Key);
                }

                if (tasks.Count <= size)
                {
                    break;
                }

                tasks.Add(tcs.Task);
                await Task.WhenAny(tasks);
                for (var i = 0; i < tasks.Count; i++)
                {
                    var task = tasks[i];
                    if (task.IsCompleted
                        && _queue.TryRemove(task, out _))
                    {
                        continue;
                    }

                    tasks.RemoveAt(i--);
                }

                await Task.WhenAll(tasks);
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
