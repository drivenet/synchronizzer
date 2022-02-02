using System;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class RetryingObjectSource : IObjectSource
    {
        private readonly IObjectSource _inner;
        private readonly byte _retries;

        public RetryingObjectSource(IObjectSource inner, byte retries)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _retries = retries;
        }

        public async Task<ObjectsBatch> GetOrdered(string? continuationToken, CancellationToken cancellationToken)
        {
            var retries = _retries;
            while (true)
            {
                try
                {
                    return await _inner.GetOrdered(continuationToken, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (TimeoutException)
                {
                }
#pragma warning disable CA1031 // Do not catch general exception types -- dumb retry mechanism
                catch when (retries-- > 0)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                }

                await Task.Delay(1511, cancellationToken);
            }
        }
    }
}
