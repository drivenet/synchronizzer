using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal interface ISynchronizer
    {
        Task Synchronize(CancellationToken cancellationToken);
    }
}
