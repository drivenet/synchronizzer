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
            var localInfos = new ObjectInfos();
            var remoteInfos = new ObjectInfos();
            try
            {
                while (remoteInfos.IsLive || localInfos.IsLive)
                {
                    await Task.WhenAll(
                        localInfos.Populate(_localReader, cancellationToken),
                        remoteInfos.Populate(_remoteWriter, cancellationToken));
                    await SynchronizeLocal(localInfos, remoteInfos, cancellationToken);
                    await SynchronizeRemote(localInfos, remoteInfos, cancellationToken);
                }
            }
            finally
            {
                await _remoteWriter.Flush(cancellationToken);
            }
        }

        private async Task SynchronizeLocal(ObjectInfos localInfos, ObjectInfos remoteInfos, CancellationToken cancellationToken)
        {
            foreach (var objectInfo in localInfos)
            {
                var name = objectInfo.Name;
                if (remoteInfos.LastName is string lastName
                    && string.CompareOrdinal(name, lastName) > 0)
                {
                    break;
                }

                if (!remoteInfos.HasObject(objectInfo))
                {
#pragma warning disable CA2000 // Dispose objects before losing scope -- expected to be disposed by Upload
                    var input = await _localReader.Read(name, cancellationToken);
#pragma warning restore CA2000 // Dispose objects before losing scope
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

                localInfos.Skip();
            }
        }

        private async Task SynchronizeRemote(ObjectInfos localInfos, ObjectInfos remoteInfos, CancellationToken cancellationToken)
        {
            foreach (var objectInfo in remoteInfos)
            {
                var name = objectInfo.Name;
                if (localInfos.LastName is string lastName
                    && string.CompareOrdinal(name, lastName) > 0)
                {
                    break;
                }

                if (!localInfos.HasObjectByName(objectInfo))
                {
                    await _remoteWriter.Delete(name, cancellationToken);
                }

                remoteInfos.Skip();
            }
        }
    }
}
