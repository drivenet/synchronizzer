using System;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class DeferringSynchronizer : ISynchronizer
    {
        private readonly ISynchronizer _inner;

        public DeferringSynchronizer(ISynchronizer inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public async Task Synchronize(CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            await _inner.Synchronize(cancellationToken);
        }
    }
}
