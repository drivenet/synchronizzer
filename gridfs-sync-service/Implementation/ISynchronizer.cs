using System.Threading;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal interface ISynchronizer
    {
        Task Synchronize(CancellationToken cancellationToken);
    }
}
