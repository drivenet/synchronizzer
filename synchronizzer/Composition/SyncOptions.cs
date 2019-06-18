using System.Collections.Generic;

namespace Synchronizzer.Composition
{
    internal sealed class SyncOptions
    {
        public IReadOnlyCollection<SyncJob>? Jobs { get; set; }

        public byte MaxParallelism { get; set; }
    }
}
