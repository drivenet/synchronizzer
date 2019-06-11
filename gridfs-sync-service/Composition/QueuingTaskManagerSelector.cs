using System;
using System.Collections.Concurrent;

using GridFSSyncService.Implementation;

namespace GridFSSyncService.Composition
{
    internal sealed class QueuingTaskManagerSelector : IQueuingTaskManagerSelector, IDisposable
    {
        private static readonly Func<string, Lazy<QueuingTaskManager>> ManagerFactory =
            _ => new Lazy<QueuingTaskManager>(() => new QueuingTaskManager());

        private readonly ConcurrentDictionary<string, Lazy<QueuingTaskManager>> _managers =
            new ConcurrentDictionary<string, Lazy<QueuingTaskManager>>();

        public void Dispose()
        {
            foreach (var manager in _managers.Values)
            {
                if (manager.IsValueCreated)
                {
                    manager.Value.Dispose();
                }
            }
        }

        public IQueuingTaskManager Select(string key)
            => _managers.GetOrAdd(key, ManagerFactory).Value;
    }
}
