using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using GridFSSyncService.Implementation;

namespace GridFSSyncService.Tests.Implementation
{
    internal sealed class ObjectSourceStub : IObjectSource, IEnumerable<ObjectInfo>
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

        public IEnumerator<ObjectInfo> GetEnumerator() => (_list ?? Enumerable.Empty<ObjectInfo>()).GetEnumerator();

        public Task<IReadOnlyCollection<ObjectInfo>> GetOrdered(string? fromName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_list is null)
            {
                return EmptyTask;
            }

            int index;
            if (fromName is object)
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
            const int BatchSize = 1000;
            if (count > BatchSize)
            {
                count = BatchSize;
            }

            if (count == 0)
            {
                return EmptyTask;
            }

            return Task.FromResult<IReadOnlyCollection<ObjectInfo>>(_list.GetRange(index, count));
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
