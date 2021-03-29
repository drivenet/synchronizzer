using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal interface IDestinationWriter : IObjectSource, IObjectWriter
    {
        string Address { get; }

        Task TryLock(CancellationToken cancellationToken);

        Task Unlock();
    }
}
