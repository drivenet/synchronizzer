using System;

namespace GridFSSyncService.Composition
{
    internal sealed class SyncJob
    {
        public string? Name { get; set; }

        public Uri? Local { get; set; }

        public Uri? Remote { get; set; }

        public Uri? Recycle { get; set; }
    }
}
