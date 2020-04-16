using System;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class RetryingObjectWriterLocker : IObjectWriterLocker
    {
        private readonly IObjectWriterLocker _inner;
        private readonly byte _retries;

        public RetryingObjectWriterLocker(IObjectWriterLocker inner, byte retries)
        {
            _inner = inner;
            _retries = retries;
        }

        public async Task Clear(CancellationToken cancellationToken)
        {
            var retries = _retries;
            while (true)
            {
                try
                {
                    await _inner.Clear(cancellationToken);
                    break;
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

        public async Task Lock(CancellationToken cancellationToken)
        {
            var retries = _retries;
            while (true)
            {
                try
                {
                    await _inner.Lock(cancellationToken);
                    break;
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
