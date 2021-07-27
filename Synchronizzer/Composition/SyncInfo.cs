using System;

namespace Synchronizzer.Composition
{
    internal sealed class SyncInfo : IEquatable<SyncInfo>
    {
        public SyncInfo(string name, string origin, string destination, string? recycle)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Origin = origin ?? throw new ArgumentNullException(nameof(origin));
            Destination = destination ?? throw new ArgumentNullException(nameof(destination));
            Recycle = recycle;
        }

        public string Name { get; }

        public string Origin { get; }

        public string Destination { get; }

        public string? Recycle { get; }

        public bool Equals(SyncInfo? other)
            => other is not null
            && Name == other.Name
            && Origin == other.Origin
            && Destination == other.Destination
            && Recycle == other.Recycle;

        public override bool Equals(object? obj) => Equals(obj as SyncInfo);

        public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);

        public override string ToString() => Name;
    }
}
