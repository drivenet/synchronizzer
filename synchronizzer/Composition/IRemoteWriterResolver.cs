using GridFSSyncService.Implementation;

namespace GridFSSyncService.Composition
{
    internal interface IRemoteWriterResolver
    {
        IRemoteWriter Resolve(string address, string? recycleAddress);
    }
}
