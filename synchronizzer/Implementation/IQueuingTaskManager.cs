using System;
using System.Threading;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal interface IQueuingTaskManager
    {
        Task Enqueue(object sender, Func<CancellationToken, Task> action, CancellationToken cancellationToken);

        Task WaitAll(object sender);
    }
}
