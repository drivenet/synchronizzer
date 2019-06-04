using System;

using GridFSSyncService.Implementation;

using Microsoft.Extensions.Logging;

namespace GridFSSyncService.Composition
{
    internal sealed class SynchronizerBuilder : ISynchronizerBuilder
    {
        private readonly ILogger<TracingSynchronizer> _logger;

        public SynchronizerBuilder(ILogger<TracingSynchronizer> logger)
        {
            _logger = logger;
        }

        public ISynchronizer Build(SyncJob job)
        {
            if (job.Name is null)
            {
                throw new ArgumentNullException(nameof(job), "Missing job name.");
            }

            if (job.Local is null)
            {
                throw new ArgumentNullException(nameof(job), "Missing job local address.");
            }

            if (job.Remote is null)
            {
                throw new ArgumentNullException(nameof(job), "Missing job remote address.");
            }

            var filesystemContext = new FilesystemContext(job.Local.LocalPath);
            var s3Context = S3Utils.CreateContext(job.Remote);
            var synchronizer = new RobustSynchronizer(
                new TracingSynchronizer(
                    new Synchronizer(
                        new FilesystemObjectSource(filesystemContext),
                        new FilesystemObjectReader(filesystemContext),
                        new S3ObjectSource(s3Context),
                        new S3ObjectWriter(s3Context)),
                    _logger,
                    job.Name));
            return synchronizer;
        }
    }
}
