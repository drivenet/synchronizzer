using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal sealed class FilesystemObjectSource : IObjectSource
    {
        public async Task<IReadOnlyCollection<ObjectInfo>> GetOrdered(string? fromName, CancellationToken cancellationToken)
        {
            return Array.Empty<ObjectInfo>();
        }
    }
}
