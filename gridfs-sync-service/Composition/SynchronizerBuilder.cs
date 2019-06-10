using System;

using GridFSSyncService.Components;
using GridFSSyncService.Implementation;

using Microsoft.Extensions.Logging;

namespace GridFSSyncService.Composition
{
    internal sealed class SynchronizerBuilder : ISynchronizerBuilder
    {
        private readonly ILogger<TracingSynchronizer> _syncLogger;
        private readonly ILogger<TracingObjectWriter> _objectLogger;
        private readonly IMetricsWriter _metricsWriter;

        public SynchronizerBuilder(ILogger<TracingSynchronizer> syncLogger, ILogger<TracingObjectWriter> objectLogger, IMetricsWriter metricsWriter)
        {
            _syncLogger = syncLogger;
            _objectLogger = objectLogger;
            _metricsWriter = metricsWriter;
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

        private ILocalReader CreateLocalReader(Uri uri)
        {
            var context = FilesystemUtils.CreateContext(uri);
            return new LocalReader(
                new CountingObjectSource(
                    new FilesystemObjectSource(context),
                    _metricsWriter,
                    "fs_local"),
                new FilesystemObjectReader(context));
        }

        private IRemoteWriter CreateRemoteWriter(Uri uri)
        {
            var context = S3Utils.CreateContext(uri);
            return new RemoteWriter(
                new CountingObjectSource(
                    new S3ObjectSource(context),
                    _metricsWriter,
                    "s3_remote"),
                new QueuingObjectWriter(
                    new RobustObjectWriter(
                        new TracingObjectWriter(
                            new CountingObjectWriter(
                                new S3ObjectWriter(context),
                                _metricsWriter),
                            _objectLogger))));
        }
    }
}
