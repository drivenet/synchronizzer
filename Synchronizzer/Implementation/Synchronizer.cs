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
        private readonly ILogger? _logger;

        public Synchronizer(IOriginReader originReader, IDestinationWriter destinationWriter, IQueuingTaskManager taskManager, ILogger<Synchronizer>? logger)
        {
            _originReader = originReader ?? throw new ArgumentNullException(nameof(originReader));
            _destinationWriter = destinationWriter ?? throw new ArgumentNullException(nameof(destinationWriter));
            _taskManager = taskManager ?? throw new ArgumentNullException(nameof(taskManager));
            _logger = logger;
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
            string? lastName = null;
            var originInfos = new ObjectInfos(_originReader, lastName);
            var destinationInfos = new ObjectInfos(_destinationWriter, lastName);
            while (true)
            {
                _logger?.LogDebug(Events.Populating, "Populating infos.");
                await Task.WhenAll(
                    originInfos.Populate(cancellationToken),
                    destinationInfos.Populate(cancellationToken));
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
                if (!objectInfo.IsHidden
                    && !destinationInfos.HasObject(objectInfo))
                {
                    await _taskManager.Enqueue(
                        this,
                        token => Upload(name, token),
                        cancellationToken);
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
                if (!objectInfo.IsHidden
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
            public static readonly EventId Populating = new EventId(1, nameof(Populating));
            public static readonly EventId Populated = new EventId(2, nameof(Populated));
            public static readonly EventId SynchronizingOrigin = new EventId(3, nameof(SynchronizingOrigin));
            public static readonly EventId SynchronizedOrigin = new EventId(4, nameof(SynchronizedOrigin));
            public static readonly EventId SynchronizeOriginFailed = new EventId(5, nameof(SynchronizeOriginFailed));
            public static readonly EventId SynchronizingDestination = new EventId(6, nameof(SynchronizingDestination));
            public static readonly EventId SynchronizedDestination = new EventId(7, nameof(SynchronizedDestination));
            public static readonly EventId SynchronizeDestinationFailed = new EventId(8, nameof(SynchronizeDestinationFailed));
        }
    }
}
