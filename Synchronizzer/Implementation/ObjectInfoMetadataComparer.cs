using System.Collections.Generic;

namespace Synchronizzer.Implementation
{
    internal sealed class ObjectInfoMetadataComparer : IComparer<ObjectInfo>
    {
        public static ObjectInfoMetadataComparer Instance { get; } = new ObjectInfoMetadataComparer();

        public int Compare(ObjectInfo? x, ObjectInfo? y)
        {
            if (y is null)
            {
                if (x is null)
                {
                    return 0;
                }

                return 1;
            }
            else
            {
                if (x is null)
                {
                    return -1;
                }
            }

            var result = string.CompareOrdinal(x.Name, y.Name);
            if (result == 0)
            {
                result = x.Size.CompareTo(y.Size);
            }

            return result;
        }
    }
}
