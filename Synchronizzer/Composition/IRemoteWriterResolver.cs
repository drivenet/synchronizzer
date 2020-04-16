using Synchronizzer.Implementation;

namespace Synchronizzer.Composition
{
    internal interface IRemoteWriterResolver
    {
        IRemoteWriter Resolve(string address, string? recycleAddress);
    }
}
