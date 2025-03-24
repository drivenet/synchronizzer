using System;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal interface ISynchronizer : IDisposable
    {
        Task Synchronize(CancellationToken cancellationToken);
    }
}
