using System;

using Microsoft.Extensions.Logging;

using Synchronizzer.Implementation;

namespace Synchronizzer.Composition
{
    internal sealed class SynchronizerFactory : ISynchronizerFactory
    {
        private readonly ILogger<TracingSynchronizer> _syncLogger;
        private readonly ILogger<Synchronizer> _synchronizerLogger;
        private readonly IOriginReaderResolver _originReaderResolver;
        private readonly IDestinationWriterResolver _destinationWriterResolver;
        private readonly IQueuingTaskManagerSelector _taskManagerSelector;
        private readonly ISyncTimeHolderResolver _syncTimeHolderResolver;

        public SynchronizerFactory(
            ILogger<TracingSynchronizer> syncLogger,
            ILogger<Synchronizer> synchronizerLogger,
            IOriginReaderResolver originReaderResolver,
            IDestinationWriterResolver destinationWriterResolver,
            IQueuingTaskManagerSelector taskManagerSelector,
            ISyncTimeHolderResolver syncTimeHolderResolver)
        {
            _syncLogger = syncLogger ?? throw new ArgumentNullException(nameof(syncLogger));
            _synchronizerLogger = synchronizerLogger ?? throw new ArgumentNullException(nameof(synchronizerLogger));
            _originReaderResolver = originReaderResolver ?? throw new ArgumentNullException(nameof(originReaderResolver));
            _destinationWriterResolver = destinationWriterResolver ?? throw new ArgumentNullException(nameof(destinationWriterResolver));
            _taskManagerSelector = taskManagerSelector ?? throw new ArgumentNullException(nameof(taskManagerSelector));
            _syncTimeHolderResolver = syncTimeHolderResolver ?? throw new ArgumentNullException(nameof(syncTimeHolderResolver));
        }

        public ISynchronizer Create(SyncInfo info)
        {
            var originReader = _originReaderResolver.Resolve(info.Origin);
            var destinationWriter = _destinationWriterResolver.Resolve(info.Destination, info.Recycle, info.DryRun);
            var taskManager = _taskManagerSelector.Select(destinationWriter.Address);
            var synchronizer = Create(info.Name, originReader, destinationWriter, taskManager);
            return synchronizer;
        }

        private ISynchronizer Create(string name, IOriginReader originReader, IDestinationWriter destinationWriter, IQueuingTaskManager taskManager)
            => new DeferringSynchronizer(
                new TimedSynchronizer(
                    new RobustSynchronizer(
                        new TracingSynchronizer(
                            new Synchronizer(originReader, destinationWriter, taskManager, _synchronizerLogger),
                            _syncLogger,
                            name)),
                    _syncTimeHolderResolver.Resolve(name)));
    }
}
