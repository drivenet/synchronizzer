using System;
using System.Collections;
using System.Collections.Generic;

using GridFSSyncService.Implementation;

using Microsoft.Extensions.Options;

namespace GridFSSyncService.Composition
{
    internal sealed class SynchronizersResolver : IEnumerable<ISynchronizer>
    {
        private readonly IOptionsMonitor<SyncOptions> _options;
        private readonly ISynchronizerFactory _factory;

        public SynchronizersResolver(IOptionsMonitor<SyncOptions> options, ISynchronizerFactory factory)
        {
            _options = options;
            _factory = factory;
        }

        public IEnumerator<ISynchronizer> GetEnumerator()
        {
            var options = _options.CurrentValue;
            foreach (var job in options.Jobs ?? Array.Empty<SyncJob>())
            {
                var synchronizer = _factory.Create(job);
                yield return synchronizer;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
