using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal interface IRemoteWriter : IObjectSource, IObjectWriter
    {
        Task TryLock(CancellationToken cancellationToken);
    }
}
