using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal interface IObjectSource
    {
        Task<ObjectsBatch> GetOrdered(string? continuationToken, CancellationToken cancellationToken);
    }
}
