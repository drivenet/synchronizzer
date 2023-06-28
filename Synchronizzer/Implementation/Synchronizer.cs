using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Synchronizzer.Implementation
{
    internal sealed class Synchronizer : ISynchronizer
    {
        private readonly IOriginReader _originReader;
        private readonly IDestinationWriter _destinationWriter;
        private readonly IQueuingTaskManager _taskManager;
        private readonly bool _copyOnly;
        private readonly bool _ignoreTimestamp;
        private readonly bool _nice;
        private readonly ILogger? _logger;
        private readonly ILogger<ObjectInfos>? _objectLogger;

        public Synchronizer(
            IOriginReader originReader,
            IDestinationWriter destinationWriter,
            IQueuingTaskManager taskManager,
            bool copyOnly,
            bool ignoreTimestamp,
            bool nice,
            ILogger<Synchronizer>? logger,
            ILogger<ObjectInfos>? objectLogger)
        {
            _originReader = originReader ?? throw new ArgumentNullException(nameof(originReader));
            _destinationWriter = destinationWriter ?? throw new ArgumentNullException(nameof(destinationWriter));
            _taskManager = taskManager ?? throw new ArgumentNullException(nameof(taskManager));
            _copyOnly = copyOnly;
            _ignoreTimestamp = ignoreTimestamp;
            _nice = nice;
            _logger = logger;
            _objectLogger = objectLogger;
        }

        public async Task Synchronize(CancellationToken cancellationToken)
        {
            await _destinationWriter.TryLock(cancellationToken);
            try
            {
                await SynchronizeCore(cancellationToken);
            }
            finally
            {
                try
                {
                    await _taskManager.WaitAll(this);
                }
                finally
                {
                    await _destinationWriter.Unlock();
                }
            }
        }

        private async Task SynchronizeCore(CancellationToken cancellationToken)
        {
            await using var originInfos = new ObjectInfos(_originReader, _objectLogger, cancellationToken);
            await using var destinationInfos = new ObjectInfos(_destinationWriter, _objectLogger, cancellationToken);
            while (true)
            {
                _logger?.LogDebug(Events.Populating, "Populating infos.");
                await Task.WhenAll(
                    originInfos.Populate(_nice),
                    destinationInfos.Populate(_nice));
                _logger?.LogDebug(Events.Populated, "Populated infos.");
                var counts = await Task.WhenAll(
                    SynchronizeOrigin(originInfos, destinationInfos, cancellationToken),
                    SynchronizeDestination(originInfos, destinationInfos, cancellationToken));

                if (!destinationInfos.IsLive && !originInfos.IsLive)
                {
                    break;
                }

                if (counts[0] == 0 && counts[1] == 0)
                {
                    throw new InvalidProgramException(
                        FormattableString.Invariant($"No progress for iteration.\nOrigin: {originInfos}\nDestination: {destinationInfos}\n"));
                }
            }
        }

        private async Task<uint> SynchronizeOrigin(ObjectInfos originInfos, ObjectInfos destinationInfos, CancellationToken cancellationToken)
        {
            _logger?.LogDebug(Events.SynchronizingOrigin, "Synchronizing origin, last name: \"{LastName}\".", originInfos.LastName);
            uint count;
            try
            {
                count = await SynchronizeOriginCore(originInfos, destinationInfos, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger?.LogWarning(Events.SynchronizeOriginFailed, exception, "Failed to synchronize origin.");
                throw;
            }

            _logger?.LogInformation(Events.SynchronizedOrigin, "Synchronized origin, count: {Count}, last name: \"{LastName}\".", count, originInfos.LastName);
            return count;
        }

        private async Task<uint> SynchronizeOriginCore(ObjectInfos originInfos, ObjectInfos destinationInfos, CancellationToken cancellationToken)
        {
            var count = 0u;
            foreach (var objectInfo in originInfos)
            {
                var name = objectInfo.Name;
                if (destinationInfos.LastName is string lastName
                    && string.CompareOrdinal(name, lastName) > 0)
                {
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();
                if (!objectInfo.IsHidden)
                {
                    var destinationObjectInfo = destinationInfos.FindObjectByMetadata(objectInfo);
                    if (destinationObjectInfo is null
                        || (!_ignoreTimestamp && objectInfo.Timestamp > destinationObjectInfo.Timestamp))
                    {
                        await _taskManager.Enqueue(
                            this,
                            token => Upload(name, token),
                            cancellationToken);
                    }
                }

                originInfos.Skip();
                ++count;
            }

            return count;
        }

        private async Task Upload(string name, CancellationToken cancellationToken)
        {
            using var input = await _originReader.Read(name, cancellationToken);
            if (input is not null)
            {
                await _destinationWriter.Upload(name, input, cancellationToken);
            }
        }

        private async Task<uint> SynchronizeDestination(ObjectInfos originInfos, ObjectInfos destinationInfos, CancellationToken cancellationToken)
        {
            _logger?.LogDebug(Events.SynchronizingDestination, "Synchronizing destination, last name: \"{LastName}\".", destinationInfos.LastName);
            uint count;
            try
            {
                count = await SynchronizeDestinationCore(originInfos, destinationInfos, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger?.LogWarning(Events.SynchronizeDestinationFailed, exception, "Failed to synchronize destination.");
                throw;
            }

            _logger?.LogInformation(Events.SynchronizedDestination, "Synchronized destination, count: {Count}, last name: \"{LastName}\".", count, destinationInfos.LastName);
            return count;
        }

        private async Task<uint> SynchronizeDestinationCore(ObjectInfos originInfos, ObjectInfos destinationInfos, CancellationToken cancellationToken)
        {
            var count = 0u;
            foreach (var objectInfo in destinationInfos)
            {
                var name = objectInfo.Name;
                if (originInfos.LastName is string lastName
                    && string.CompareOrdinal(name, lastName) > 0)
                {
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();
                if (!_copyOnly
                    && !objectInfo.IsHidden
                    && !originInfos.HasObjectByName(objectInfo))
                {
                    await _taskManager.Enqueue(
                        this,
                        token => _destinationWriter.Delete(name, token),
                        cancellationToken);
                }

                destinationInfos.Skip();
                ++count;
            }

            return count;
        }

        private static class Events
        {
            public static readonly EventId Populating = new(1, nameof(Populating));
            public static readonly EventId Populated = new(2, nameof(Populated));
            public static readonly EventId SynchronizingOrigin = new(3, nameof(SynchronizingOrigin));
            public static readonly EventId SynchronizedOrigin = new(4, nameof(SynchronizedOrigin));
            public static readonly EventId SynchronizeOriginFailed = new(5, nameof(SynchronizeOriginFailed));
            public static readonly EventId SynchronizingDestination = new(6, nameof(SynchronizingDestination));
            public static readonly EventId SynchronizedDestination = new(7, nameof(SynchronizedDestination));
            public static readonly EventId SynchronizeDestinationFailed = new(8, nameof(SynchronizeDestinationFailed));
        }
    }
}
