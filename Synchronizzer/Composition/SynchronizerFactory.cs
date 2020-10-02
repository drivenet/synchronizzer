using Microsoft.Extensions.Logging;

using Synchronizzer.Implementation;

namespace Synchronizzer.Composition
{
    internal sealed class SynchronizerFactory : ISynchronizerFactory
    {
        private readonly ILogger<TracingSynchronizer> _syncLogger;
        private readonly ILogger<Synchronizer> _synchronizerLogger;
        private readonly ILocalReaderResolver _localReaderResolver;
        private readonly IRemoteWriterResolver _remoteWriterResolver;
        private readonly IQueuingTaskManagerSelector _taskManagerSelector;
        private readonly ISyncTimeHolderResolver _syncTimeHolderResolver;

        public SynchronizerFactory(
            ILogger<TracingSynchronizer> syncLogger,
            ILogger<Synchronizer> synchronizerLogger,
            ILocalReaderResolver localReaderResolver,
            IRemoteWriterResolver remoteWriterResolver,
            IQueuingTaskManagerSelector taskManagerSelector,
            ISyncTimeHolderResolver syncTimeHolderResolver)
        {
            _syncLogger = syncLogger ?? throw new System.ArgumentNullException(nameof(syncLogger));
            _synchronizerLogger = synchronizerLogger ?? throw new System.ArgumentNullException(nameof(synchronizerLogger));
            _localReaderResolver = localReaderResolver ?? throw new System.ArgumentNullException(nameof(localReaderResolver));
            _remoteWriterResolver = remoteWriterResolver ?? throw new System.ArgumentNullException(nameof(remoteWriterResolver));
            _taskManagerSelector = taskManagerSelector ?? throw new System.ArgumentNullException(nameof(taskManagerSelector));
            _syncTimeHolderResolver = syncTimeHolderResolver ?? throw new System.ArgumentNullException(nameof(syncTimeHolderResolver));
        }

        public ISynchronizer Create(SyncInfo info)
        {
            var localReader = _localReaderResolver.Resolve(info.Local);
            var remoteWriter = _remoteWriterResolver.Resolve(info.Remote, info.Recycle);
            var taskManager = _taskManagerSelector.Select(remoteWriter.Address);
            var synchronizer = Create(info.Name, localReader, remoteWriter, taskManager);
            return synchronizer;
        }

        private ISynchronizer Create(string name, ILocalReader localReader, IRemoteWriter remoteWriter, IQueuingTaskManager taskManager)
            => new TimedSynchronizer(
                new RobustSynchronizer(
                    new TracingSynchronizer(
                        new Synchronizer(localReader, remoteWriter, taskManager, _synchronizerLogger),
                        _syncLogger,
                        name)),
                _syncTimeHolderResolver.Resolve(name));
    }
}
