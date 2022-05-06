using System;
using System.Globalization;

namespace Synchronizzer.Implementation
{
    internal sealed class ObjectInfo : IComparable<ObjectInfo>
    {
        public ObjectInfo(string name, long size, bool isHidden, DateTime timestamp)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(name), name, "Empty object name.");
            }

            if (size < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), size, FormattableString.Invariant($"Negative object size for \"{name}\"."));
            }

            if (timestamp.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentOutOfRangeException(nameof(timestamp), timestamp, FormattableString.Invariant($"Invalid timestamp for \"{name}\"."));
            }

            Name = name;
            Size = size;
            IsHidden = isHidden;
            Timestamp = timestamp;
        }

        public string Name { get; }

        public long Size { get; }

        public bool IsHidden { get; }

        public DateTime Timestamp { get; }

        public int CompareTo(ObjectInfo? other)
        {
            if (other is null)
            {
                return 1;
            }

            var result = string.CompareOrdinal(Name, other.Name);
            if (result == 0)
            {
                result = Size.CompareTo(other.Size);
                if (result == 0)
                {
                    result = IsHidden.CompareTo(other.IsHidden);
                    if (result == 0)
                    {
                        result = Timestamp.CompareTo(other.Timestamp);
                    }
                }
            }

            return result;
        }

        public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);

        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "\"{0}\" {1}{2}", Name, Size, IsHidden ? ", hidden" : null);
    }
}
