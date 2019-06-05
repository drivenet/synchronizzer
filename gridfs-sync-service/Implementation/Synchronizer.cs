using System.Threading;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
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
            while (remoteInfos.IsLive && localInfos.IsLive)
            {
                await Task.WhenAll(
                    localInfos.Populate(_localReader, cancellationToken),
                    remoteInfos.Populate(_remoteWriter, cancellationToken));

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
                        using (var input = await _localReader.Read(name, cancellationToken))
                        {
                            await _remoteWriter.Upload(name, input, cancellationToken);
                        }
                    }

                    localInfos.Skip();
                }

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

            await _remoteWriter.Flush(cancellationToken);
        }
    }
}
