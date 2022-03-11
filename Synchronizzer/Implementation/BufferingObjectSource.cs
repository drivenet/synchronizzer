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
        const int BufferSize = 8192;
        List<ObjectInfo>? buffer = null;
        await foreach (var infos in _inner.GetOrdered(cancellationToken))
        {
            (buffer ??= new(BufferSize)).AddRange(infos);
            if (buffer.Count > BufferSize)
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
