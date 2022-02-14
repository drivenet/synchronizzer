using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Synchronizzer.Implementation;

internal sealed class BufferingObjectSource : IObjectSource
{
    private readonly IObjectSource _inner;

    public BufferingObjectSource(IObjectSource inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public async IAsyncEnumerable<IReadOnlyCollection<ObjectInfo>> GetOrdered([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var enumerator = _inner.GetOrdered(cancellationToken).GetAsyncEnumerator(cancellationToken);
        List<ObjectInfo>? buffer = null;
        var enumerationTask = enumerator.MoveNextAsync();
        while (await enumerationTask)
        {
            var current = enumerator.Current;
            enumerationTask = enumerator.MoveNextAsync();
            (buffer ??= new()).AddRange(current);
            if (buffer.Count > 8192)
            {
                yield return buffer;
                buffer = null;
            }
        }

        if (buffer is { Count: not 0 })
        {
            yield return buffer;
        }
    }
}
