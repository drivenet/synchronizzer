using System;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class Synchronizer : ISynchronizer
    {
        private readonly ILocalReader _localReader;
        private readonly IRemoteWriter _remoteWriter;

        public Synchronizer(ILocalReader localReader, IRemoteWriter remoteWriter)
        {
            _localReader = localReader;
            _remoteWriter = remoteWriter;
        }

        public async Task Synchronize(CancellationToken cancellationToken)
        {
            await _remoteWriter.TryLock(cancellationToken);
            string? lastName = null;
            var localInfos = new ObjectInfos(_localReader, lastName);
            var remoteInfos = new ObjectInfos(_remoteWriter, lastName);
            try
            {
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
            finally
            {
                await _remoteWriter.Flush(cancellationToken);
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
#pragma warning disable CA2000 // Dispose objects before losing scope -- expected to be disposed by Upload
                    var input = await _localReader.Read(name, cancellationToken);
#pragma warning restore CA2000 // Dispose objects before losing scope
                    if (input is object)
                    {
                        try
                        {
                            await _remoteWriter.Upload(name, input, cancellationToken);
                        }
                        catch
                        {
                            input.Dispose();
                            throw;
                        }
                    }
                }

                localInfos.Skip();
                hasProgress = true;
            }

            return hasProgress;
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
                    await _remoteWriter.Delete(name, cancellationToken);
                }

                remoteInfos.Skip();
                hasProgress = true;
            }

            return hasProgress;
        }
    }
}
