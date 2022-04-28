namespace Synchronizzer.Composition
{
    internal sealed class SyncJob
    {
        public string? Name { get; set; }

        public string? Origin { get; set; }

        public string? Destination { get; set; }

        public string? Recycle { get; set; }

        public string? ExcludePattern { get; set; }

        public bool DryRun { get; set; }

        public bool CopyOnly { get; set; }
    }
}
