using System;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class Synchronizer : ISynchronizer
    {
        private readonly ILocalReader _localReader;
        private readonly IRemoteWriter _remoteWriter;
        private readonly IQueuingTaskManager _taskManager;

        public Synchronizer(ILocalReader localReader, IRemoteWriter remoteWriter, IQueuingTaskManager taskManager)
        {
            _localReader = localReader;
            _remoteWriter = remoteWriter;
            _taskManager = taskManager;
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
                await Task.WhenAll(
                    localInfos.Populate(cancellationToken),
                    remoteInfos.Populate(cancellationToken));
                var hasProgress = await Task.WhenAll(
                    SynchronizeLocal(localInfos, remoteInfos, cancellationToken),
                    SynchronizeRemote(localInfos, remoteInfos, cancellationToken));

                if (!remoteInfos.IsLive && !localInfos.IsLive)
                {
                    break;
                }

                if (!hasProgress[0] && !hasProgress[1])
                {
                    throw new InvalidProgramException(
                        FormattableString.Invariant($"No progress for iteration.\nLocal: {localInfos}\nRemote: {remoteInfos}\n"));
                }
            }
        }

        private async Task<bool> SynchronizeLocal(ObjectInfos localInfos, ObjectInfos remoteInfos, CancellationToken cancellationToken)
        {
            var hasProgress = false;
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
                hasProgress = true;
            }

            return hasProgress;
        }

        private async Task Upload(string name, CancellationToken cancellationToken)
        {
            using var input = await _localReader.Read(name, cancellationToken);
            if (input is object)
            {
                await _remoteWriter.Upload(name, input, cancellationToken);
            }
        }

        private async Task<bool> SynchronizeRemote(ObjectInfos localInfos, ObjectInfos remoteInfos, CancellationToken cancellationToken)
        {
            var hasProgress = false;
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
                hasProgress = true;
            }

            return hasProgress;
        }
    }
}
