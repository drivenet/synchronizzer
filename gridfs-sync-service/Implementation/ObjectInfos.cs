using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GridFSSyncService.Implementation
{
    internal sealed class ObjectInfos : IEnumerable<ObjectInfo>
    {
        private const int MaxListLength = 8192;
        private static readonly IComparer<ObjectInfo> NameOnlyComparer = new ObjectInfoNameComparer();

        private List<ObjectInfo>? _infos = new List<ObjectInfo>();
        private string? _lastName;
        private int _skip;

        public string? LastName
        {
            get
            {
                if (_infos is null)
                {
                    throw CreateCompletedException();
                }

                return _lastName;
            }
        }

        public bool IsLive => _infos is object;

        public bool HasFreeSpace => _infos?.Count < MaxListLength || _skip != 0;

        private List<ObjectInfo> Infos
        {
            get
            {
                if (_infos is null)
                {
                    throw CreateCompletedException();
                }

                return _infos;
            }
        }

        public void Add(IEnumerable<ObjectInfo> newInfos)
        {
            if (_infos is null)
            {
                throw CreateCompletedException();
            }

            _infos.RemoveRange(0, _skip);
            _skip = 0;
            _infos.AddRange(newInfos);
            var count = _infos.Count;
            if (count == 0)
            {
                _infos = null;
                return;
            }

            _lastName = _infos[count - 1].Name;
        }

        public IEnumerator<ObjectInfo> GetEnumerator() => (_infos ?? Enumerable.Empty<ObjectInfo>()).GetEnumerator();

        public void Skip()
        {
            if (_infos is null)
            {
                throw CreateCompletedException();
            }

            ++_skip;
        }

        public bool HasObject(ObjectInfo objectInfo)
        {
            var infos = Infos;
            var index = infos.BinarySearch(0, infos.Count, objectInfo, null);
            return index >= 0;
        }

        public bool HasObjectByName(ObjectInfo objectInfo)
        {
            var infos = Infos;
            var index = infos.BinarySearch(0, infos.Count, objectInfo, NameOnlyComparer);
            return index >= 0;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private static InvalidOperationException CreateCompletedException() => new InvalidOperationException("Object infos are marked as completed.");
    }
}
