using System;

using Microsoft.Extensions.Logging;

using Synchronizzer.Implementation;

namespace Synchronizzer.Composition
{
    internal sealed class SynchronizerFactory : ISynchronizerFactory
    {
        private readonly ILogger<TracingSynchronizer> _syncLogger;
        private readonly ILogger<Synchronizer> _synchronizerLogger;
        private readonly ILogger<ObjectInfos> _objectLogger;
        private readonly IOriginReaderResolver _originReaderResolver;
        private readonly IDestinationWriterResolver _destinationWriterResolver;
        private readonly IQueuingTaskManagerSelector _taskManagerSelector;
        private readonly ISyncTimeHolderResolver _syncTimeHolderResolver;
        private readonly TimeProvider _timeProvider;

        public SynchronizerFactory(
            ILogger<TracingSynchronizer> syncLogger,
            ILogger<Synchronizer> synchronizerLogger,
            ILogger<ObjectInfos> objectLogger,
            IOriginReaderResolver originReaderResolver,
            IDestinationWriterResolver destinationWriterResolver,
            IQueuingTaskManagerSelector taskManagerSelector,
            ISyncTimeHolderResolver syncTimeHolderResolver,
            TimeProvider timeProvider)
        {
            _syncLogger = syncLogger ?? throw new ArgumentNullException(nameof(syncLogger));
            _synchronizerLogger = synchronizerLogger ?? throw new ArgumentNullException(nameof(synchronizerLogger));
            _objectLogger = objectLogger ?? throw new ArgumentNullException(nameof(objectLogger));
            _originReaderResolver = originReaderResolver ?? throw new ArgumentNullException(nameof(originReaderResolver));
            _destinationWriterResolver = destinationWriterResolver ?? throw new ArgumentNullException(nameof(destinationWriterResolver));
            _taskManagerSelector = taskManagerSelector ?? throw new ArgumentNullException(nameof(taskManagerSelector));
            _syncTimeHolderResolver = syncTimeHolderResolver ?? throw new ArgumentNullException(nameof(syncTimeHolderResolver));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public ISynchronizer Create(SyncInfo info)
        {
            IDestinationWriter? destinationWriter = null;
            IOriginReader? originReader = null;
            try
            {
                originReader = _originReaderResolver.Resolve(info.Origin);
                if (info.Exclude is { } exclude)
                {
                    var filteringSource = new FilteringObjectSource(originReader, exclude);
                    originReader = new OriginReader(filteringSource, originReader, originReader, originReader.Address);
                }

                destinationWriter = _destinationWriterResolver.Resolve(info.Destination, info.Recycle, info.DryRun);
                var taskManager = _taskManagerSelector.Select(destinationWriter.Address);
                var synchronizer = Create(info.Name, originReader, destinationWriter, taskManager, info.CopyOnly, info.IgnoreTimestamp, info.Nice);
                return synchronizer;
            }
            catch
            {
                destinationWriter?.Dispose();
                originReader?.Dispose();
                throw;
            }
        }

        private ISynchronizer Create(
            string name,
            IOriginReader originReader,
            IDestinationWriter destinationWriter,
            IQueuingTaskManager taskManager,
            bool copyOnly,
            bool ignoreTimestamp,
            bool nice)
            => new DeferringSynchronizer(
                new TimedSynchronizer(
                    new RobustSynchronizer(
                        new TracingSynchronizer(
                            new Synchronizer(originReader, destinationWriter, taskManager, copyOnly, ignoreTimestamp, nice, _synchronizerLogger, _objectLogger),
                            _syncLogger,
                            name)),
                    _syncTimeHolderResolver.Resolve(name),
                    _timeProvider));
    }
}
