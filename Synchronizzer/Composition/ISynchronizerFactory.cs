using Synchronizzer.Implementation;

namespace Synchronizzer.Composition
{
    internal interface ISynchronizerFactory
    {
        ISynchronizer Create(SyncInfo info);
    }
}
