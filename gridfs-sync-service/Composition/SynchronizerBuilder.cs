using System;

using GridFSSyncService.Implementation;

using Microsoft.Extensions.Logging;

namespace GridFSSyncService.Composition
{
    internal sealed class SynchronizerBuilder : ISynchronizerBuilder
    {
        private readonly ILogger<ISynchronizer> _logger;

        public SynchronizerBuilder(ILogger<ISynchronizer> logger)
        {
            _logger = logger;
        }

        public ISynchronizer Build(SyncJob job)
        {
            if (job.Remote == null)
            {
                throw new ArgumentNullException(nameof(job.Remote));
            }

            var s3Context = S3Utils.CreateContext(job.Remote);
            var synchronizer = new RobustSynchronizer(
                new TracingSynchronizer(
                    new Synchronizer(
                        new FilesystemObjectSource(),
                        new FilesystemObjectReader(),
                        new S3ObjectSource(s3Context),
                        new S3ObjectWriter(s3Context)),
                    _logger));
            return synchronizer;
        }
    }
}
