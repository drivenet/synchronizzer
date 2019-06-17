using GridFSSyncService.Implementation;

namespace GridFSSyncService.Composition
{
    internal interface ISyncTimeHolderResolver
    {
        SyncTimeHolder Resolve(string name);
    }
}