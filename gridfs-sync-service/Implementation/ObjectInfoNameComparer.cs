using System;
using System.Collections.Generic;

namespace GridFSSyncService.Implementation
{
    internal sealed class ObjectInfoNameComparer : IComparer<ObjectInfo>
    {
        public static ObjectInfoNameComparer Instance { get; } = new ObjectInfoNameComparer();

        public int Compare(ObjectInfo x, ObjectInfo y)
            => string.Compare(x?.Name, y?.Name, StringComparison.Ordinal);
    }
}
