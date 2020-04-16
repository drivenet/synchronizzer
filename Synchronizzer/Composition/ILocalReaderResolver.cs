using Synchronizzer.Implementation;

namespace Synchronizzer.Composition
{
    internal interface ILocalReaderResolver
    {
        ILocalReader Resolve(string address);
    }
}
