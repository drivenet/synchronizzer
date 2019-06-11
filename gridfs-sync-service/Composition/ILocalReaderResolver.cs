using GridFSSyncService.Implementation;

namespace GridFSSyncService.Composition
{
    internal interface ILocalReaderResolver
    {
        ILocalReader Resolve(string address);
    }
}
