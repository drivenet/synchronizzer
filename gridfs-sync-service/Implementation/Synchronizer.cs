using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal sealed class Synchronizer
    {
        private static readonly Task<IReadOnlyCollection<ObjectInfo>> EmptyTask = Task.FromResult<IReadOnlyCollection<ObjectInfo>>(Array.Empty<ObjectInfo>());

        private readonly IObjectSource _localSource;
        private readonly IObjectSource _remoteSource;
        private readonly IObjectManager _objectManager;

        public Synchronizer(IObjectSource localSource, IObjectSource remoteSource, IObjectManager objectManager)
        {
            _localSource = localSource;
            _remoteSource = remoteSource;
            _objectManager = objectManager;
        }

        public async void Synchronize()
        {
            const ushort BatchSize = 1000;
            const ushort MaxListLength = 10000;
            string? lastLocalObjectName = null;
            string? lastRemoteObjectName = null;
            var localList = new List<ObjectInfo>();
            var remoteList = new List<ObjectInfo>();
            var comparer = new ObjectInfoNameComparer();
            while (true)
            {
                var localTask = localList.Count < MaxListLength - BatchSize
                    ? _localSource.GetObjects(lastLocalObjectName, BatchSize)
                    : EmptyTask;
                var remoteTask = remoteList.Count < MaxListLength - BatchSize
                    ? _remoteSource.GetObjects(lastRemoteObjectName, BatchSize)
                    : EmptyTask;
                localList.AddRange(await localTask);
                lastLocalObjectName = localList.Count > 0 ? localList[localList.Count - 1].Name : null;
                remoteList.AddRange(await remoteTask);
                lastRemoteObjectName = remoteList.Count > 0 ? remoteList[remoteList.Count - 1].Name : null;
                if (lastLocalObjectName == null && lastRemoteObjectName == null)
                {
                    break;
                }

                var localProcessedCount = 0;
                foreach (var objectInfo in localList)
                {
                    if (lastRemoteObjectName != null
                        && string.Compare(objectInfo.Name, lastRemoteObjectName, StringComparison.Ordinal) > 0)
                    {
                        break;
                    }

                    var index = remoteList.BinarySearch(objectInfo);
                    if (index < 0)
                    {
                        await _objectManager.Upload(objectInfo.Name, null);
                    }

                    ++localProcessedCount;
                }

                var remoteProcessedCount = 0;
                foreach (var objectInfo in localList)
                {
                    if (lastLocalObjectName != null
                        && string.Compare(objectInfo.Name, lastLocalObjectName, StringComparison.Ordinal) > 0)
                    {
                        break;
                    }

                    var index = localList.BinarySearch(objectInfo, comparer);
                    if (index < 0)
                    {
                        await _objectManager.Delete(objectInfo.Name);
                    }

                    ++remoteProcessedCount;
                }

                localList.RemoveRange(0, localProcessedCount);
                remoteList.RemoveRange(0, remoteProcessedCount);
            }

            await _objectManager.Flush();
        }
    }
}
