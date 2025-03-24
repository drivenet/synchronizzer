using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Synchronizzer.Implementation;

namespace Synchronizzer.Composition
{
    internal sealed class TracingRootSynchronizer : ISynchronizer
    {
        private readonly ISynchronizer _inner;
        private readonly ILogger _logger;

        public TracingRootSynchronizer(ISynchronizer inner, ILogger<TracingRootSynchronizer> logger)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Synchronize(CancellationToken cancellationToken)
        {
            try
            {
                await _inner.Synchronize(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Synchronization failed.");
                throw;
            }
        }

        public void Dispose() => _inner.Dispose();
    }
}
