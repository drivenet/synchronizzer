using System;
using System.Collections.Generic;
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

        public async Task Synchronize()
        {
            const ushort BatchSize = 1000;
            const ushort MaxListLength = 10000;
            string? lastLocalName = null;
            string? lastRemoteName = null;
            var localList = (List<ObjectInfo>?)new List<ObjectInfo>();
            var remoteList = (List<ObjectInfo>?)new List<ObjectInfo>();
            var comparer = new ObjectInfoNameComparer();
            while (true)
            {
                var localTask = localList?.Count < MaxListLength - BatchSize
                    ? _localSource.GetObjects(lastLocalName, BatchSize)
                    : null;

                var remoteTask = remoteList?.Count < MaxListLength - BatchSize
                    ? _remoteSource.GetObjects(lastRemoteName, BatchSize)
                    : null;

                var localProcessedCount = 0;

                if (localList != null)
                {
                    if (localTask != null)
                    {
                        localList.AddRange(await localTask);
                    }

                    if (localList.Count > 0)
                    {
                        lastLocalName = localList[localList.Count - 1].Name;

                        foreach (var objectInfo in localList)
                        {
                            var name = objectInfo.Name;
                            if (lastRemoteName != null
                                && string.Compare(name, lastRemoteName, StringComparison.Ordinal) > 0)
                            {
                                break;
                            }

                            var index = remoteList?.BinarySearch(objectInfo) ?? -1;
                            if (index < 0)
                            {
                                using (var input = await _localReader.Read(name))
                                {
                                    await _remoteWriter.Upload(name, input);
                                }
                            }

                            ++localProcessedCount;
                        }
                    }
                    else
                    {
                        localList = null;
                        lastLocalName = null;
                    }
                }

                if (remoteList != null)
                {
                    if (remoteTask != null)
                    {
                        remoteList.AddRange(await remoteTask);
                    }

                    if (remoteList.Count > 0)
                    {
                        lastRemoteName = remoteList[remoteList.Count - 1].Name;
                        var remoteProcessedCount = 0;
                        foreach (var objectInfo in remoteList)
                        {
                            var name = objectInfo.Name;
                            if (lastLocalName != null
                                && string.Compare(name, lastLocalName, StringComparison.Ordinal) > 0)
                            {
                                break;
                            }

                            var index = localList?.BinarySearch(objectInfo, comparer) ?? -1;
                            if (index < 0)
                            {
                                await _remoteWriter.Delete(name);
                            }

                            ++remoteProcessedCount;
                        }

                        remoteList.RemoveRange(0, remoteProcessedCount);
                    }
                    else
                    {
                        remoteList = null;
                        lastRemoteName = null;
                    }
                }

                if (localList != null)
                {
                    localList.RemoveRange(0, localProcessedCount);
                }
                else
                {
                    if (remoteList == null)
                    {
                        break;
                    }
                }
            }

            await _remoteWriter.Flush();
        }
    }
}
