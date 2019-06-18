using Synchronizzer.Implementation;

namespace Synchronizzer.Composition
{
    internal interface ISyncTimeHolderResolver
    {
        SyncTimeHolder Resolve(string name);
    }
}
