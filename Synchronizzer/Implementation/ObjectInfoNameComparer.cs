using System.Collections.Generic;

namespace Synchronizzer.Implementation
{
    internal sealed class ObjectInfoNameComparer : IComparer<ObjectInfo>
    {
        public static ObjectInfoNameComparer Instance { get; } = new ObjectInfoNameComparer();

        public int Compare(ObjectInfo x, ObjectInfo y)
            => string.CompareOrdinal(x?.Name, y?.Name);
    }
}
