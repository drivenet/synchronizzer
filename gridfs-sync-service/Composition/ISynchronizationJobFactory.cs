using System.Threading;

namespace GridFSSyncService.Composition
{
    internal interface ISynchronizationJobFactory
    {
        SynchronizationJob Create(SyncInfo info, CancellationToken cancellationToken);
    }
}