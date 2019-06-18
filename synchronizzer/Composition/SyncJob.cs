using System;

namespace GridFSSyncService.Composition
{
    internal sealed class SyncJob
    {
        public string? Name { get; set; }

        public string? Local { get; set; }

        public string? Remote { get; set; }

        public string? Recycle { get; set; }
    }
}
