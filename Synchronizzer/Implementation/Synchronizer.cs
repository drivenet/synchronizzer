using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Synchronizzer.Implementation
{
    internal sealed class Synchronizer : ISynchronizer
    {
        private readonly ILocalReader _localReader;
        private readonly IRemoteWriter _remoteWriter;
        private readonly IQueuingTaskManager _taskManager;
        private readonly ILogger? _logger;

        public Synchronizer(ILocalReader localReader, IRemoteWriter remoteWriter, IQueuingTaskManager taskManager, ILogger<Synchronizer>? logger)
        {
            _localReader = localReader ?? throw new ArgumentNullException(nameof(localReader));
            _remoteWriter = remoteWriter ?? throw new ArgumentNullException(nameof(remoteWriter));
            _taskManager = taskManager ?? throw new ArgumentNullException(nameof(taskManager));
            _logger = logger;
        }

        public async Task Synchronize(CancellationToken cancellationToken)
        {
            await _remoteWriter.TryLock(cancellationToken);
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
                    await _remoteWriter.Unlock();
                }
            }
        }

        private async Task SynchronizeCore(CancellationToken cancellationToken)
        {
            string? lastName = null;
            var localInfos = new ObjectInfos(_localReader, lastName);
            var remoteInfos = new ObjectInfos(_remoteWriter, lastName);
            while (true)
            {
                _logger?.LogDebug(Events.Populating, "Populating infos.");
                await Task.WhenAll(
                    localInfos.Populate(cancellationToken),
                    remoteInfos.Populate(cancellationToken));
                _logger?.LogDebug(Events.Populated, "Populated infos.");
                var counts = await Task.WhenAll(
                    SynchronizeLocal(localInfos, remoteInfos, cancellationToken),
                    SynchronizeRemote(localInfos, remoteInfos, cancellationToken));

                if (!remoteInfos.IsLive && !localInfos.IsLive)
                {
                    break;
                }

                if (counts[0] == 0 && counts[1] == 0)
                {
                    throw new InvalidProgramException(
                        FormattableString.Invariant($"No progress for iteration.\nLocal: {localInfos}\nRemote: {remoteInfos}\n"));
                }
            }
        }

        private async Task<uint> SynchronizeLocal(ObjectInfos localInfos, ObjectInfos remoteInfos, CancellationToken cancellationToken)
        {
            _logger?.LogDebug(Events.SynchronizingLocal, "Synchronizing local, last name: \"{LastName}\".", localInfos.LastName);
            uint count;
            try
            {
                count = await SynchronizeLocalCore(localInfos, remoteInfos, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger?.LogWarning(Events.SynchronizeLocalFailed, exception, "Failed to synchronize local.");
                throw;
            }

            _logger?.LogInformation(Events.SynchronizedLocal, "Synchronized local, count: {Count}, last name: \"{LastName}\".", count, localInfos.LastName);
            return count;
        }

        private async Task<uint> SynchronizeLocalCore(ObjectInfos localInfos, ObjectInfos remoteInfos, CancellationToken cancellationToken)
        {
            var count = 0u;
            foreach (var objectInfo in localInfos)
            {
                var name = objectInfo.Name;
                if (remoteInfos.LastName is string lastName
                    && string.CompareOrdinal(name, lastName) > 0)
                {
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();
                if (!objectInfo.IsHidden
                    && !remoteInfos.HasObject(objectInfo))
                {
                    await _taskManager.Enqueue(
                        this,
                        token => Upload(name, token),
                        cancellationToken);
                }

                localInfos.Skip();
                ++count;
            }

            return count;
        }

        private async Task Upload(string name, CancellationToken cancellationToken)
        {
            using var input = await _localReader.Read(name, cancellationToken);
            if (input is object)
            {
                await _remoteWriter.Upload(name, input, cancellationToken);
            }
        }

        private async Task<uint> SynchronizeRemote(ObjectInfos localInfos, ObjectInfos remoteInfos, CancellationToken cancellationToken)
        {
            _logger?.LogDebug(Events.SynchronizingRemote, "Synchronizing remote, last name: \"{LastName}\".", remoteInfos.LastName);
            uint count;
            try
            {
                count = await SynchronizeRemoteCore(localInfos, remoteInfos, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger?.LogWarning(Events.SynchronizeRemoteFailed, exception, "Failed to synchronize remote.");
                throw;
            }

            _logger?.LogInformation(Events.SynchronizedRemote, "Synchronized remote, count: {Count}, last name: \"{LastName}\".", count, remoteInfos.LastName);
            return count;
        }

        private async Task<uint> SynchronizeRemoteCore(ObjectInfos localInfos, ObjectInfos remoteInfos, CancellationToken cancellationToken)
        {
            var count = 0u;
            foreach (var objectInfo in remoteInfos)
            {
                var name = objectInfo.Name;
                if (localInfos.LastName is string lastName
                    && string.CompareOrdinal(name, lastName) > 0)
                {
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();
                if (!objectInfo.IsHidden
                    && !localInfos.HasObjectByName(objectInfo))
                {
                    await _taskManager.Enqueue(
                        this,
                        token => _remoteWriter.Delete(name, token),
                        cancellationToken);
                }

                remoteInfos.Skip();
                ++count;
            }

            return count;
        }

        private static class Events
        {
            public static readonly EventId Populating = new EventId(1, nameof(Populating));
            public static readonly EventId Populated = new EventId(2, nameof(Populated));
            public static readonly EventId SynchronizingLocal = new EventId(3, nameof(SynchronizingLocal));
            public static readonly EventId SynchronizedLocal = new EventId(4, nameof(SynchronizedLocal));
            public static readonly EventId SynchronizeLocalFailed = new EventId(5, nameof(SynchronizeLocalFailed));
            public static readonly EventId SynchronizingRemote = new EventId(6, nameof(SynchronizingRemote));
            public static readonly EventId SynchronizedRemote = new EventId(7, nameof(SynchronizedRemote));
            public static readonly EventId SynchronizeRemoteFailed = new EventId(8, nameof(SynchronizeRemoteFailed));
        }
    }
}
