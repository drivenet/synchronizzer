using System;
using System.Collections;
using System.Collections.Generic;

namespace Synchronizzer.Implementation;

internal sealed class ObjectsBatch : IReadOnlyCollection<ObjectInfo>
{
    public static readonly ObjectsBatch Empty = new(Array.Empty<ObjectInfo>(), "");

    private readonly IReadOnlyCollection<ObjectInfo> _objects;

    public ObjectsBatch(IReadOnlyCollection<ObjectInfo> objects, string continuationToken)
    {
        _objects = objects ?? throw new ArgumentNullException(nameof(objects));
        ContinuationToken = continuationToken;
    }

    public string ContinuationToken { get; }

    public int Count => _objects.Count;

    public IEnumerator<ObjectInfo> GetEnumerator() => _objects.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_objects).GetEnumerator();
}
