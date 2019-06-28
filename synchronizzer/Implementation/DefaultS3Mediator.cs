using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Amazon.S3;

namespace Synchronizzer.Implementation
{
    internal sealed class DefaultS3Mediator : IS3Mediator
    {
        private readonly IAmazonS3 _s3;

        public DefaultS3Mediator(IAmazonS3 s3)
        {
            _s3 = s3;
        }

        public string ServiceUrl => _s3.Config.DetermineServiceURL();

        public Task Invoke(Func<IAmazonS3, CancellationToken, Task> action, CancellationToken cancellationToken)
            => action(_s3, cancellationToken);

        public Task<TResult> Invoke<TResult>(Func<IAmazonS3, CancellationToken, Task<TResult>> action, CancellationToken cancellationToken)
            => action(_s3, cancellationToken);
    }
}
