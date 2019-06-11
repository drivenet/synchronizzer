using System;

using GridFSSyncService.Components;
using GridFSSyncService.Implementation;

using Microsoft.Extensions.Logging;

namespace GridFSSyncService.Composition
{
    internal sealed class SynchronizerFactory : ISynchronizerFactory
    {
        private readonly ILogger<TracingSynchronizer> _syncLogger;
        private readonly ILogger<TracingObjectWriter> _objectLogger;
        private readonly IMetricsWriter _metricsWriter;

        public SynchronizerFactory(ILogger<TracingSynchronizer> syncLogger, ILogger<TracingObjectWriter> objectLogger, IMetricsWriter metricsWriter)
        {
            _syncLogger = syncLogger;
            _objectLogger = objectLogger;
            _metricsWriter = metricsWriter;
        }

        public ISynchronizer Create(SyncJob job)
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

        private ILocalReader CreateLocalReader(string address)
        {
            if (Uri.TryCreate(address, UriKind.Absolute, out var uri))
            {
                var context = FilesystemUtils.CreateContext(uri);
                return new LocalReader(
                    new CountingObjectSource(
                        new FilesystemObjectSource(context),
                        _metricsWriter,
                        "local.fs"),
                    new FilesystemObjectReader(context));
            }
            else if (address.StartsWith("mongodb://", StringComparison.OrdinalIgnoreCase))
            {
                var context = GridFSUtils.CreateContext(address);
                return new LocalReader(
                    new CountingObjectSource(
                        new GridFSObjectSource(context),
                        _metricsWriter,
                        "local.gridfs"),
                    new GridFSObjectReader(context));
            }

            throw new ArgumentOutOfRangeException(nameof(address), "Invalid local address.");
        }

        private IRemoteWriter CreateRemoteWriter(string address)
        {
            if (Uri.TryCreate(address, UriKind.Absolute, out var uri))
            {
                var context = S3Utils.CreateContext(uri);
                return new RemoteWriter(
                    new CountingObjectSource(
                        new S3ObjectSource(context),
                        _metricsWriter,
                        "remote.s3"),
                    new QueuingObjectWriter(
                        new RobustObjectWriter(
                            new TracingObjectWriter(
                                new CountingObjectWriter(
                                    new S3ObjectWriter(context),
                                    _metricsWriter),
                                _objectLogger))));
            }

            throw new ArgumentOutOfRangeException(nameof(address), "Invalid remote address.");
        }
    }
}
