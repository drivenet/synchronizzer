using System;

namespace GridFSSyncService.Composition
{
    internal sealed class SyncInfo : IEquatable<SyncInfo>
    {
        public SyncInfo(string name, string local, string remote, string? recycle)
        {
            Name = name;
            Local = local;
            Remote = remote;
            Recycle = recycle;
        }

        public string Name { get; }

        public string Local { get; }

        public string Remote { get; }

        public string? Recycle { get; }

#pragma warning disable CS8614 // Nullability of reference types in type of parameter doesn't match implicitly implemented member. -- matches, just not yet
        public bool Equals(SyncInfo? other)
#pragma warning restore CS8614 // Nullability of reference types in type of parameter doesn't match implicitly implemented member.
            => other is object
            && Name == other.Name
            && Local == other.Local
            && Remote == other.Remote
            && Recycle == other.Recycle;

        public override bool Equals(object obj) => Equals(obj as SyncInfo);

        public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);

        public override string ToString() => Name;
    }
}
