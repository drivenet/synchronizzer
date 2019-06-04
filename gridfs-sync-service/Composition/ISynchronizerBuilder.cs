using GridFSSyncService.Implementation;

namespace GridFSSyncService.Composition
{
    internal interface ISynchronizerBuilder
    {
        ISynchronizer Build(SyncJob job);
    }
}
