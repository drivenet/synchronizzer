using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.S3;

namespace Synchronizzer.Implementation
{
    internal sealed class CancelationHandlingS3Mediator : IS3Mediator
    {
        private readonly IS3Mediator _inner;

        public CancelationHandlingS3Mediator(IS3Mediator inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public Uri ServiceUrl => _inner.ServiceUrl;

        public async Task<TResult> Invoke<TResult>(Func<IAmazonS3, CancellationToken, Task<TResult>> action, FormattableString description, CancellationToken cancellationToken)
        {
            try
            {
                return await _inner.Invoke(action, description, cancellationToken);
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException("S3 operation timed out.", exception);
            }
        }

        public async Task Cleanup(Func<IAmazonS3, Task> action, FormattableString description)
        {
            try
            {
                await _inner.Cleanup(action, description);
            }
            catch (OperationCanceledException exception)
            {
                throw new TimeoutException("S3 cleanup operation timed out.", exception);
            }
        }
    }
}
