using System;

using GridFSSyncService.Implementation;

using Microsoft.Extensions.Logging;

namespace GridFSSyncService.Composition
{
    internal sealed class SynchronizerFactory : ISynchronizerFactory
    {
        private readonly ILogger<TracingSynchronizer> _syncLogger;
        private readonly ILocalReaderResolver _localReaderResolver;
        private readonly IRemoteWriterResolver _remoteWriterResolver;

        public SynchronizerFactory(ILogger<TracingSynchronizer> syncLogger, ILocalReaderResolver localReaderResolver, IRemoteWriterResolver remoteWriterResolver)
        {
            _syncLogger = syncLogger;
            _localReaderResolver = localReaderResolver;
            _remoteWriterResolver = remoteWriterResolver;
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

            var localReader = _localReaderResolver.Resolve(job.Local);
            var remoteWriter = _remoteWriterResolver.Resolve(job.Remote);
            var synchronizer = Create(job.Name, localReader, remoteWriter);
            return synchronizer;
        }

        private ISynchronizer Create(string name, ILocalReader localReader, IRemoteWriter remoteWriter)
            => new RobustSynchronizer(
                new TracingSynchronizer(
                    new Synchronizer(localReader, remoteWriter),
                    _syncLogger,
                    name));
    }
}
