using System;
using System.Collections.Concurrent;

using Synchronizzer.Implementation;

namespace Synchronizzer.Composition
{
    internal sealed class SyncTimeHolderResolver : ISyncTimeHolderResolver
    {
        private readonly ConcurrentDictionary<string, Lazy<SyncTimeHolder>> _cache = new();

        public static SyncTimeHolderResolver Instance { get; } = new SyncTimeHolderResolver();

        public SyncTimeHolder Resolve(string name)
            => _cache.GetOrAdd(name, _ => new Lazy<SyncTimeHolder>()).Value;
    }
}
