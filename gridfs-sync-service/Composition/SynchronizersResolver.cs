using System;
using System.Collections;
using System.Collections.Generic;

using GridFSSyncService.Implementation;

using Microsoft.Extensions.Options;

namespace GridFSSyncService.Composition
{
    internal sealed class SynchronizersResolver : IEnumerable<ISynchronizer>
    {
        private readonly IOptions<SyncOptions> _options;
        private readonly ISynchronizerFactory _factory;

        public SynchronizersResolver(IOptions<SyncOptions> options, ISynchronizerFactory factory)
        {
            _options = options;
            _factory = factory;
        }

        public IEnumerator<ISynchronizer> GetEnumerator()
        {
            var options = _options.Value;
            foreach (var job in options.Jobs ?? Array.Empty<SyncJob>())
            {
                var synchronizer = _factory.Create(job);
                yield return synchronizer;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
