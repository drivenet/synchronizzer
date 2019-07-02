using System;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class RetryingObjectWriterLocker : IObjectWriterLocker
    {
        private readonly IObjectWriterLocker _inner;

        public RetryingObjectWriterLocker(IObjectWriterLocker inner)
        {
            _inner = inner;
        }

        public async Task Clear(CancellationToken cancellationToken)
        {
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

                await Task.Delay(1511, cancellationToken);
            }
        }

        public async Task Lock(CancellationToken cancellationToken)
        {
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

                await Task.Delay(1511, cancellationToken);
            }
        }
    }
}
