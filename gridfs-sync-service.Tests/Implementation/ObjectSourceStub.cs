using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using GridFSSyncService.Implementation;

namespace GridFSSyncService.Tests.Implementation
{
    internal sealed class ObjectSourceStub : IObjectSource
    {
        private static readonly Task<IReadOnlyCollection<ObjectInfo>> EmptyTask = Task.FromResult<IReadOnlyCollection<ObjectInfo>>(Array.Empty<ObjectInfo>());

        private readonly List<ObjectInfo>? _list;

        public ObjectSourceStub(IEnumerable<ObjectInfo> infos)
        {
            _list = new List<ObjectInfo>(infos);
            if (_list.Count == 0)
            {
                _list = null;
                return;
            }

            _list.Sort();
            string? previous = null;
            for (var i = 0; i < _list.Count; i++)
            {
                var current = _list[i].Name;
                if (current == previous)
                {
                    throw new ArgumentOutOfRangeException(nameof(infos), current, "Non-unique object name encountered.");
                }

                previous = current;
            }
        }

        public Task<IReadOnlyCollection<ObjectInfo>> GetObjects(string? fromName, ushort batchSize)
        {
            if (_list == null || batchSize == 0)
            {
                return EmptyTask;
            }

            int index;
            if (fromName != null)
            {
                index = _list.BinarySearch(new ObjectInfo(fromName, 0), ObjectInfoNameComparer.Instance);
                if (index < 0)
                {
                    index = ~index;
                }
                else
                {
                    ++index;
                }
            }
            else
            {
                index = 0;
            }

            var count = _list.Count - index;
            if (count > batchSize)
            {
                count = batchSize;
            }

            if (count == 0)
            {
                return EmptyTask;
            }

            return Task.FromResult<IReadOnlyCollection<ObjectInfo>>(_list.GetRange(index, count));
        }
    }
}
