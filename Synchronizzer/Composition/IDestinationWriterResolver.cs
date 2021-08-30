using Synchronizzer.Implementation;

namespace Synchronizzer.Composition
{
    internal interface IDestinationWriterResolver
    {
        IDestinationWriter Resolve(string address, string? recycleAddress, bool dryRun);
    }
}
