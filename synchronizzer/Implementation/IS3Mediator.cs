using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;

namespace Synchronizzer.Implementation
{
    internal interface IS3Mediator
    {
        string ServiceUrl { get; }

        Task Invoke(Func<IAmazonS3, CancellationToken, Task> action, CancellationToken cancellationToken);
        Task<TResult> Invoke<TResult>(Func<IAmazonS3, CancellationToken, Task<TResult>> action, CancellationToken cancellationToken);
    }
}