using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

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

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async IAsyncEnumerable<IReadOnlyCollection<ObjectInfo>> GetOrdered(bool nice, [EnumeratorCancellation] CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_list is null)
            {
                yield break;
            }

            var index = 0;
            while (true)
            {
                var count = _list.Count - index;
                if (count == 0)
                {
                    break;
                }

                const int BatchSize = 1000;
                if (count > BatchSize)
                {
                    count = BatchSize;
                }

                yield return _list.GetRange(index, count);
                index += count;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
