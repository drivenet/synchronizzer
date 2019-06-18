using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class ObjectInfos : IEnumerable<ObjectInfo>
    {
        private const int MaxListLength = 8192;
        private static readonly IComparer<ObjectInfo> NameOnlyComparer = new ObjectInfoNameComparer();

        private List<ObjectInfo>? _infos = new List<ObjectInfo>();
        private int _skip;

        public string? LastName { get; private set; }

        public bool IsLive => _infos is object;

        public async Task Populate(IObjectSource source, CancellationToken cancellationToken)
        {
            if (_infos is null)
            {
                return;
            }

            ObjectInfo? lastInfo = _infos.LastOrDefault();
            _infos.RemoveRange(0, _skip);
            _skip = 0;
            if (_infos.Count >= MaxListLength)
            {
                return;
            }

            var newInfos = await source.GetOrdered(LastName, cancellationToken);
            foreach (var info in newInfos)
            {
                if (info.CompareTo(lastInfo) <= 0)
                {
                    throw new InvalidDataException(FormattableString.Invariant($"Current object info {info} is not sorted wrt {lastInfo}."));
                }

                _infos.Add(info);
                lastInfo = info;
            }

            var count = _infos.Count;
            if (count == 0)
            {
                _infos = null;
                LastName = null;
                return;
            }

            LastName = _infos[count - 1].Name;
        }

        public IEnumerator<ObjectInfo> GetEnumerator() => (_infos ?? Enumerable.Empty<ObjectInfo>()).GetEnumerator();

        public void Skip()
        {
            if (_infos is null)
            {
                throw CreateCompletedException();
            }

            var count = _infos.Count;
            if (_skip >= count)
            {
                throw new InvalidOperationException(FormattableString.Invariant($"Skip {_skip} is greater than or equal to count {count}"));
            }

            ++_skip;
        }

        public bool HasObject(ObjectInfo objectInfo)
        {
            if (_infos == null)
            {
                return false;
            }

            var index = _infos.BinarySearch(objectInfo, null);
            return index >= 0;
        }

        public bool HasObjectByName(ObjectInfo objectInfo)
        {
            if (_infos == null)
            {
                return false;
            }

            var index = _infos.BinarySearch(objectInfo, NameOnlyComparer);
            return index >= 0;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private static InvalidOperationException CreateCompletedException() => new InvalidOperationException("Object infos are marked as completed.");
    }
}
