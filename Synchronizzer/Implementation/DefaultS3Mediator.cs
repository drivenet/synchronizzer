using System;
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
            _s3 = s3 ?? throw new ArgumentNullException(nameof(s3));
#pragma warning disable CS0618 // Type or member is obsolete -- required for implementation
            ServiceUrl = new Uri(_s3.Config.DetermineServiceURL(), UriKind.Absolute);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public Uri ServiceUrl { get; }

        public Task<TResult> Invoke<TResult>(Func<IAmazonS3, CancellationToken, Task<TResult>> action, FormattableString description, CancellationToken cancellationToken)
            => action(_s3, cancellationToken);

        public Task Cleanup(Func<IAmazonS3, Task> action, FormattableString description)
            => action(_s3);
    }
}
