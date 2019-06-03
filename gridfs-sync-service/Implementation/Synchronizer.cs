using System.Threading;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal sealed class Synchronizer
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
                var localTask = localInfos.HasFreeSpace
                    ? _localSource.GetObjects(localInfos.LastName, cancellationToken)
                    : null;

                var remoteTask = remoteInfos.HasFreeSpace
                    ? _remoteSource.GetObjects(remoteInfos.LastName, cancellationToken)
                    : null;

                if (localTask is object)
                {
                    localInfos.Add(await localTask);
                }

                if (remoteTask is object)
                {
                    remoteInfos.Add(await remoteTask);
                }

                foreach (var objectInfo in localInfos)
                {
                    var name = objectInfo.Name;
                    bool upload;
                    if (remoteInfos.IsLive)
                    {
                        if (string.CompareOrdinal(name, remoteInfos.LastName) > 0)
                        {
                            break;
                        }

                        upload = !remoteInfos.HasObject(objectInfo);
                    }
                    else
                    {
                        upload = true;
                    }

                    if (upload)
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
                    bool delete;
                    if (localInfos.IsLive)
                    {
                        if (string.CompareOrdinal(name, localInfos.LastName) > 0)
                        {
                            break;
                        }

                        delete = !localInfos.HasObjectByName(objectInfo);
                    }
                    else
                    {
                        delete = true;
                    }

                    if (delete)
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
