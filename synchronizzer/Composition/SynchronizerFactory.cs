using Synchronizzer.Implementation;

using Microsoft.Extensions.Logging;

namespace Synchronizzer.Composition
{
    internal sealed class SynchronizerFactory : ISynchronizerFactory
    {
        private readonly ILogger<TracingSynchronizer> _syncLogger;
        private readonly ILocalReaderResolver _localReaderResolver;
        private readonly IRemoteWriterResolver _remoteWriterResolver;
        private readonly ISyncTimeHolderResolver _syncTimeHolderResolver;

        public SynchronizerFactory(
            ILogger<TracingSynchronizer> syncLogger,
            ILocalReaderResolver localReaderResolver,
            IRemoteWriterResolver remoteWriterResolver,
            ISyncTimeHolderResolver syncTimeHolderResolver)
        {
            _syncLogger = syncLogger;
            _localReaderResolver = localReaderResolver;
            _remoteWriterResolver = remoteWriterResolver;
            _syncTimeHolderResolver = syncTimeHolderResolver;
        }

        public ISynchronizer Create(SyncInfo info)
        {
            var localReader = _localReaderResolver.Resolve(info.Local);
            var remoteWriter = _remoteWriterResolver.Resolve(info.Remote, info.Recycle);
            var synchronizer = Create(info.Name, localReader, remoteWriter);
            return synchronizer;
        }

        private ISynchronizer Create(string name, ILocalReader localReader, IRemoteWriter remoteWriter)
            => new TimedSynchronizer(
                new RobustSynchronizer(
                    new TracingSynchronizer(
                        new Synchronizer(localReader, remoteWriter),
                        _syncLogger,
                        name)),
                _syncTimeHolderResolver.Resolve(name));
    }
}
