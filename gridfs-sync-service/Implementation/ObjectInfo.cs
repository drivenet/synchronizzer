using System;
using System.Text;

namespace GridFSSyncService.Implementation
{
    internal sealed class ObjectInfo : IComparable<ObjectInfo>
    {
        public ObjectInfo(string name, long size)
        {
            if (name.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(name), name, "Empty object name.");
            }

            if (size < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), size, "Negative object size.");
            }

            Name = name.Normalize(NormalizationForm.FormC);
            Size = size;
        }

        public string Name { get; }

        public long Size { get; }

        public int CompareTo(ObjectInfo other)
            => other is null
                ? 1 :
                (Name, Size).CompareTo((other.Name, other.Size));

        public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);
    }
}
