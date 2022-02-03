using System.Collections.Generic;
using System.Threading;

namespace Synchronizzer.Implementation
{
    internal interface IObjectSource
    {
        IAsyncEnumerable<IReadOnlyCollection<ObjectInfo>> GetOrdered(CancellationToken cancellationToken);
    }
}
