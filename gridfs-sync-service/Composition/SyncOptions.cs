using System.Collections.Generic;

namespace GridFSSyncService.Composition
{
    internal sealed class SyncOptions
    {
        public IReadOnlyCollection<SyncJob>? Jobs { get; set; }
    }
}
