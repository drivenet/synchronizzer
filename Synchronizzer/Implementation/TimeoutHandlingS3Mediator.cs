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
        private readonly int _maxRetries;

        public TimeoutHandlingS3Mediator(IS3Mediator inner, TimeSpan timeout, int maxRetries)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            if (timeout.TotalMilliseconds < 1 || timeout.TotalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Invalid S3 timeout.");
            }

            if (maxRetries < 0 || maxRetries > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(maxRetries), maxRetries, "Invalid S3 timeout.");
            }

            _timeout = timeout;
            _maxRetries = maxRetries;
        }

        public string Prefix => _inner.Prefix;

        public async Task<TResult> Invoke<TResult>(Func<IAmazonS3, CancellationToken, Task<TResult>> action, FormattableString description, CancellationToken cancellationToken)
        {
            var retries = _maxRetries;
            while (true)
            {
                using var cts = new CancellationTokenSource(_timeout);
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
                try
                {
                    return await _inner.Invoke(action, description, combinedCts.Token);
                }
                catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
                {
                    if (retries <= 0)
                    {
                        throw new TimeoutException("S3 operation timed out.", exception);
                    }

                    --retries;
                }
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
