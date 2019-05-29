using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal sealed class Synchronizer
    {
        private static readonly Task<IReadOnlyCollection<ObjectInfo>> EmptyTask = Task.FromResult<IReadOnlyCollection<ObjectInfo>>(Array.Empty<ObjectInfo>());

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
            const int MaxListLength = 8192;
            string? lastLocalName = null;
            string? lastRemoteName = null;
            var localList = (List<ObjectInfo>?)new List<ObjectInfo>();
            var remoteList = (List<ObjectInfo>?)new List<ObjectInfo>();
            var comparer = new ObjectInfoNameComparer();
            while (true)
            {
                var localTask = localList?.Count < MaxListLength
                    ? _localSource.GetObjects(lastLocalName)
                    : EmptyTask;

                var remoteTask = remoteList?.Count < MaxListLength
                    ? _remoteSource.GetObjects(lastRemoteName)
                    : EmptyTask;

                if (localList is object)
                {
                    localList.AddRange(await localTask);
                    var count = localList.Count;
                    if (count > 0)
                    {
                        lastLocalName = localList[count - 1].Name;
                    }
                    else
                    {
                        localList = null;
                        lastLocalName = null;
                    }
                }

                if (remoteList is object)
                {
                    remoteList.AddRange(await remoteTask);
                    var count = remoteList.Count;
                    if (count > 0)
                    {
                        lastRemoteName = remoteList[count - 1].Name;
                    }
                    else
                    {
                        remoteList = null;
                        lastRemoteName = null;
                    }
                }

                var skipLocal = 0;
                if (localList is object)
                {
                    var lastIndex = 0;
                    foreach (var objectInfo in localList)
                    {
                        var name = objectInfo.Name;
                        if (lastRemoteName is object
                            && string.CompareOrdinal(name, lastRemoteName) > 0)
                        {
                            break;
                        }

                        var upload = false;
                        if (remoteList is object)
                        {
                            var index = remoteList.BinarySearch(lastIndex, remoteList.Count - lastIndex, objectInfo, null);
                            if (index < 0)
                            {
                                upload = true;
                                index = ~index;
                            }

                            lastIndex = index;
                        }
                        else
                        {
                            upload = true;
                        }

                        if (upload)
                        {
                            using (var input = await _localReader.Read(name))
                            {
                                await _remoteWriter.Upload(name, input);
                            }
                        }

                        ++skipLocal;
                    }
                }

                if (remoteList is object)
                {
                    var skipRemote = 0;
                    var lastIndex = 0;
                    foreach (var objectInfo in remoteList)
                    {
                        var name = objectInfo.Name;
                        if (lastLocalName is object
                            && string.CompareOrdinal(name, lastLocalName) > 0)
                        {
                            break;
                        }

                        var delete = false;
                        if (localList is object)
                        {
                            var index = localList.BinarySearch(lastIndex, localList.Count - lastIndex, objectInfo, comparer);
                            if (index < 0)
                            {
                                delete = true;
                                index = ~index;
                            }

                            lastIndex = index;
                        }
                        else
                        {
                            delete = true;
                        }

                        if (delete)
                        {
                            await _remoteWriter.Delete(name);
                        }

                        ++skipRemote;
                    }

                    remoteList.RemoveRange(0, skipRemote);
                }

                if (localList is object)
                {
                    localList.RemoveRange(0, skipLocal);
                }
                else
                {
                    if (remoteList is null)
                    {
                        break;
                    }
                }
            }

            await _remoteWriter.Flush();
        }
    }
}
