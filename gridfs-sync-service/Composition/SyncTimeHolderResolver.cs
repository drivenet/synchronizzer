using System;
using System.Collections.Concurrent;

using GridFSSyncService.Implementation;

namespace GridFSSyncService.Composition
{
    internal sealed class SyncTimeHolderResolver : ISyncTimeHolderResolver
    {
        private readonly ConcurrentDictionary<string, Lazy<SyncTimeHolder>> _cache = new ConcurrentDictionary<string, Lazy<SyncTimeHolder>>();

        public static SyncTimeHolderResolver Instance { get; } = new SyncTimeHolderResolver();

        public SyncTimeHolder Resolve(string name)
            => _cache.GetOrAdd(name, _ => new Lazy<SyncTimeHolder>()).Value;
    }
}
