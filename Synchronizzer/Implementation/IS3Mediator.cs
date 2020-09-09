using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.S3;

namespace Synchronizzer.Implementation
{
    internal interface IS3Mediator
    {
        Uri ServiceUrl { get; }

        Task<TResult> Invoke<TResult>(Func<IAmazonS3, CancellationToken, Task<TResult>> action, CancellationToken cancellationToken);

        Task Cleanup(Func<IAmazonS3, Task> action);
    }
}
