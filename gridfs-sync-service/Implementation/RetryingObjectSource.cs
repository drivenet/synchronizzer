using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal sealed class RetryingObjectSource : IObjectSource
    {
        private readonly IObjectSource _inner;
        private readonly byte _retries;

        public RetryingObjectSource(IObjectSource inner, byte retries)
        {
            _inner = inner;
            _retries = retries;
        }

        public async Task<IReadOnlyCollection<ObjectInfo>> GetOrdered(string? fromName, CancellationToken cancellationToken)
        {
            var retries = _retries;
            while (true)
            {
                try
                {
                    return await _inner.GetOrdered(fromName, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
#pragma warning disable CA1031 // Do not catch general exception types -- dumb retry mechanism
                catch when (retries-- > 0)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                }

                await Task.Delay(4000, cancellationToken);
            }
        }
    }
}
