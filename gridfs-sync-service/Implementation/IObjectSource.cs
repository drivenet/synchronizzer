using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal interface IObjectSource
    {
        Task<IReadOnlyCollection<ObjectInfo>> GetObjects(string? fromName, CancellationToken cancellationToken);
    }
}
