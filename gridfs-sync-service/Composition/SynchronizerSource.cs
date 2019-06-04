using System;
using System.Collections;
using System.Collections.Generic;

using GridFSSyncService.Implementation;

using Microsoft.Extensions.Options;

namespace GridFSSyncService.Composition
{
    internal sealed class SynchronizerSource : IEnumerable<ISynchronizer>
    {
        private readonly IOptions<SyncOptions> _options;
        private readonly ISynchronizerBuilder _builder;

        public SynchronizerSource(IOptions<SyncOptions> options, ISynchronizerBuilder builder)
        {
            _options = options;
            _builder = builder;
        }

        public IEnumerator<ISynchronizer> GetEnumerator()
        {
            var options = _options.Value;
            foreach (var job in options.Jobs ?? Array.Empty<SyncJob>())
            {
                var synchronizer = _builder.Build(job);
                yield return synchronizer;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
