using Microsoft.Extensions.Options;

using Synchronizzer.Implementation;

namespace Synchronizzer.Composition
{
    internal sealed class QueuingSettings : IQueuingSettings
    {
        private readonly IOptionsMonitor<SyncOptions> _options;

        public QueuingSettings(IOptionsMonitor<SyncOptions> options)
        {
            _options = options;
        }

        public byte MaxParallelism => _options.CurrentValue.MaxParallelism;
    }
}
