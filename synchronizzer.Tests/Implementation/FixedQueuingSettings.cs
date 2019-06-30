using Synchronizzer.Implementation;

namespace Synchronizzer.Tests.Implementation
{
    internal sealed class FixedQueuingSettings : IQueuingSettings
    {
        public byte MaxParallelism => 1;
    }
}
