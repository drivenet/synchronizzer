using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal interface IObjectSource
    {
        Task<IEnumerable<ObjectInfo>> GetOrdered(string? fromName, CancellationToken cancellationToken);
    }
}
