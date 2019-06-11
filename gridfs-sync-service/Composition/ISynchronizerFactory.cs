using GridFSSyncService.Implementation;

namespace GridFSSyncService.Composition
{
    internal interface ISynchronizerFactory
    {
        ISynchronizer Build(SyncJob job);
    }
}
