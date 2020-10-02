using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.S3;

namespace Synchronizzer.Implementation
{
    internal sealed class TimeoutHandlingS3Mediator : IS3Mediator
    {
        private readonly IS3Mediator _inner;
        private readonly TimeSpan _timeout;

        public TimeoutHandlingS3Mediator(IS3Mediator inner, TimeSpan timeout)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            if (timeout != Timeout.InfiniteTimeSpan
                && (timeout.TotalMilliseconds < 1 || timeout.TotalMilliseconds > int.MaxValue))
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Invalid S3 timeout.");
            }

            _timeout = timeout;
        }

        public Uri ServiceUrl => _inner.ServiceUrl;

        public async Task<TResult> Invoke<TResult>(Func<IAmazonS3, CancellationToken, Task<TResult>> action, CancellationToken cancellationToken)
        {
            if (_timeout != Timeout.InfiniteTimeSpan)
            {
                using var cts = new CancellationTokenSource(_timeout);
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
                return await _inner.Invoke(action, combinedCts.Token);
            }

            return await _inner.Invoke(action, cancellationToken);
        }

        public Task Cleanup(Func<IAmazonS3, Task> action)
            => _inner.Cleanup(action);
    }
}
