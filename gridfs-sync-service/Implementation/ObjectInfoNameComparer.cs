using System;
using System.Collections.Generic;

namespace GridFSSyncService.Implementation
{
    internal sealed class ObjectInfoNameComparer : IComparer<ObjectInfo>
    {
        public int Compare(ObjectInfo x, ObjectInfo y)
            => string.Compare(x?.Name, y?.Name, StringComparison.Ordinal);
    }
}
