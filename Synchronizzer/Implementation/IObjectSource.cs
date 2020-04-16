using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal interface IObjectSource
    {
        Task<IReadOnlyCollection<ObjectInfo>> GetOrdered(string? fromName, CancellationToken cancellationToken);
    }
}
