using System.Threading;

namespace Synchronizzer.Composition
{
    internal interface ISynchronizationJobFactory
    {
        SynchronizationJob Create(SyncInfo info, CancellationToken cancellationToken);
    }
}
