using System;
using System.Collections.Concurrent;

using GridFSSyncService.Implementation;

namespace GridFSSyncService.Composition
{
    internal sealed class QueuingTaskManagerSelector : IQueuingTaskManagerSelector
    {
        private static readonly Func<string, Lazy<IQueuingTaskManager>> ManagerFactory =
            _ => new Lazy<IQueuingTaskManager>(() => new QueuingTaskManager());

        private readonly ConcurrentDictionary<string, Lazy<IQueuingTaskManager>> _managers =
            new ConcurrentDictionary<string, Lazy<IQueuingTaskManager>>();

        public IQueuingTaskManager Select(string key)
            => _managers.GetOrAdd(key, ManagerFactory).Value;
    }
}
