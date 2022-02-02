using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Synchronizzer.Implementation;

namespace Synchronizzer.Tests.Implementation
{
    internal sealed class ObjectSourceStub : IObjectSource, IEnumerable<ObjectInfo>
    {
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

        public Task<ObjectsBatch> GetOrdered(string? continuationToken, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_list is null)
            {
                return Task.FromResult(ObjectsBatch.Empty);
            }

            int index;
            if (continuationToken is not null)
            {
                if (continuationToken.Length == 0)
                {
                    return Task.FromResult(ObjectsBatch.Empty);
                }

                index = int.Parse(continuationToken, NumberFormatInfo.InvariantInfo);
            }
            else
            {
                index = 0;
            }

            var count = _list.Count - index;
            if (count == 0)
            {
                return Task.FromResult(ObjectsBatch.Empty);
            }

            const int BatchSize = 1000;
            if (count > BatchSize)
            {
                count = BatchSize;
            }

            var nextIndex = index + count;
            continuationToken = nextIndex.ToString(NumberFormatInfo.InvariantInfo);
            return Task.FromResult(new ObjectsBatch(_list.GetRange(index, count), continuationToken));
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
