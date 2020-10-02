using System;
using System.Collections.Concurrent;

using Synchronizzer.Implementation;

namespace Synchronizzer.Composition
{
    internal sealed class QueuingTaskManagerSelector : IQueuingTaskManagerSelector
    {
        private readonly Func<string, Lazy<IQueuingTaskManager>> _managerFactory;

        private readonly ConcurrentDictionary<string, Lazy<IQueuingTaskManager>> _managers =
            new ConcurrentDictionary<string, Lazy<IQueuingTaskManager>>();

        public QueuingTaskManagerSelector(IQueuingSettings settings)
        {
            if (settings is null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _managerFactory =
                _ => new Lazy<IQueuingTaskManager>(() => new QueuingTaskManager(settings));
        }

        public IQueuingTaskManager Select(string key)
            => _managers.GetOrAdd(key, _managerFactory).Value;
    }
}
