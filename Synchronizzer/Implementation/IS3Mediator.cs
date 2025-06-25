using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.S3;

namespace Synchronizzer.Implementation
{
    internal interface IS3Mediator
    {
        string Prefix { get; }

        Task<TResult> Invoke<TResult>(Func<IAmazonS3, CancellationToken, Task<TResult>> action, FormattableString description, CancellationToken cancellationToken);

        Task Cleanup(Func<IAmazonS3, Task> action, FormattableString description);
    }
}
