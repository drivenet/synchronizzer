using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

        public async IAsyncEnumerable<IReadOnlyCollection<ObjectInfo>> GetOrdered([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var retries = _retries;
            IAsyncEnumerator<IReadOnlyCollection<ObjectInfo>>? enumerator = null;
            try
            {
                while (true)
                {
                    try
                    {
                        enumerator ??= _inner.GetOrdered(cancellationToken).GetAsyncEnumerator(cancellationToken);
                        if (!await enumerator.MoveNextAsync())
                        {
                            break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (TimeoutException)
                    {
                        continue;
                    }
#pragma warning disable CA1031 // Do not catch general exception types -- dumb retry mechanism
                    catch when (retries-- > 0)
#pragma warning restore CA1031 // Do not catch general exception types
                    {
                        await Task.Delay(1511, cancellationToken);
                        continue;
                    }

                    yield return enumerator.Current;
                }
            }
            finally
            {
                if (enumerator is not null)
                {
                    await enumerator.DisposeAsync();
                }
            }
        }
    }
}
