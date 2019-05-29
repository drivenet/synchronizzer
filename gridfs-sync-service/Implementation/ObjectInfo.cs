using System;
using System.Globalization;
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
        {
            if (other is null)
            {
                return 1;
            }

            var nameResult = string.CompareOrdinal(Name, other.Name);
            if (nameResult == 0)
            {
                nameResult = Size.CompareTo(other.Size);
            }

            return nameResult;
        }

        public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);

        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "\"{0}\" {1}", Name, Size);
    }
}
