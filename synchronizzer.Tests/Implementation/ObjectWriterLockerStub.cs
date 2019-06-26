using System.Threading;
using System.Threading.Tasks;

using Synchronizzer.Implementation;

namespace Synchronizzer.Tests.Implementation
{
    internal sealed class ObjectWriterLockerStub : IObjectWriterLocker
    {
        public Task Clear(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task Lock(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
