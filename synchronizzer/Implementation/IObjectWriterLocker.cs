using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal interface IObjectWriterLocker
    {
        Task Lock(CancellationToken cancellationToken);

        Task Clear(CancellationToken cancellationToken);
    }
}
