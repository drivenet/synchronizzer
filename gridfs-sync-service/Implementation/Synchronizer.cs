using System.Threading;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal sealed class Synchronizer : ISynchronizer
    {
        private readonly IObjectSource _localSource;
        private readonly IObjectReader _localReader;
        private readonly IObjectSource _remoteSource;
        private readonly IObjectWriter _remoteWriter;

        public Synchronizer(IObjectSource localSource, IObjectReader localReader, IObjectSource remoteSource, IObjectWriter remoteWriter)
        {
            _localSource = localSource;
            _localReader = localReader;
            _remoteSource = remoteSource;
            _remoteWriter = remoteWriter;
        }

        public async Task Synchronize(CancellationToken cancellationToken)
        {
            var localInfos = new ObjectInfos();
            var remoteInfos = new ObjectInfos();
            while (remoteInfos.IsLive && localInfos.IsLive)
            {
                await Task.WhenAll(
                    localInfos.Populate(_localSource, cancellationToken),
                    remoteInfos.Populate(_remoteSource, cancellationToken));

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
