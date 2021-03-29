using Synchronizzer.Implementation;

namespace Synchronizzer.Composition
{
    internal interface IOriginReaderResolver
    {
        IOriginReader Resolve(string address);
    }
}
