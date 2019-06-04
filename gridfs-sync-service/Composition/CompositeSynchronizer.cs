using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using GridFSSyncService.Implementation;

namespace GridFSSyncService.Composition
{
    internal sealed class CompositeSynchronizer : ISynchronizer
    {
        private readonly IEnumerable<ISynchronizer> _inner;

        public CompositeSynchronizer(IEnumerable<ISynchronizer> inner)
        {
            _inner = inner;
        }

        public async Task Synchronize(CancellationToken cancellationToken)
        {
            try
            {
                await Task.WhenAll(_inner.Select(inner => inner.Synchronize(cancellationToken)));
            }
            catch (AggregateException exception) when (TryDeaggregate(exception, cancellationToken, out var aggregatedToken))
            {
                throw new OperationCanceledException("The synchronization was canceled.", exception, aggregatedToken);
            }
        }

        private static bool TryDeaggregate(AggregateException exception, CancellationToken cancellationToken, out CancellationToken aggregatedToken)
        {
            var exceptions = exception.Flatten().InnerExceptions;
            CancellationToken? token = null;
            foreach (var innerException in exceptions)
            {
                if (innerException is OperationCanceledException oce)
                {
                    if (token == null
                        || (token != cancellationToken && oce.CancellationToken == cancellationToken))
                    {
                        token = oce.CancellationToken;
                    }
                }
                else
                {
                    return false;
                }
            }

            if (token != null)
            {
                aggregatedToken = (CancellationToken)token;
                return true;
            }

            return false;
        }
    }
}
