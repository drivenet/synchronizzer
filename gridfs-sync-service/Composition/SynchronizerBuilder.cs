using System;

using GridFSSyncService.Implementation;

using Microsoft.Extensions.Logging;

namespace GridFSSyncService.Composition
{
    internal sealed class SynchronizerBuilder : ISynchronizerBuilder
    {
        private readonly ILogger<TracingSynchronizer> _syncLogger;
        private readonly ILogger<TracingObjectWriter> _objectLogger;

        public SynchronizerBuilder(ILogger<TracingSynchronizer> syncLogger, ILogger<TracingObjectWriter> objectLogger)
        {
            _syncLogger = syncLogger;
            _objectLogger = objectLogger;
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

            var localReader = CreateLocalReader(job.Local);
            var remoteWriter = CreateRemoteWriter(job.Remote);
            var synchronizer = new RobustSynchronizer(
                new TracingSynchronizer(
                    new Synchronizer(localReader, remoteWriter),
                    _syncLogger,
                    job.Name));
            return synchronizer;
        }

        private static ILocalReader CreateLocalReader(Uri uri)
        {
            var context = FilesystemUtils.CreateContext(uri);
            return new LocalReader(
                new FilesystemObjectSource(context),
                new FilesystemObjectReader(context));
        }

        private IRemoteWriter CreateRemoteWriter(Uri uri)
        {
            var context = S3Utils.CreateContext(uri);
            return new RemoteWriter(
                new S3ObjectSource(context),
                new QueuingObjectWriter(
                    new TracingObjectWriter(
                        new S3ObjectWriter(context),
                        _objectLogger)));
        }
    }
}
