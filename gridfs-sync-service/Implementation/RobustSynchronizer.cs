using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace GridFSSyncService.Implementation
{
    internal sealed class RobustSynchronizer : ISynchronizer
    {
        private readonly ISynchronizer _inner;
        private readonly ILogger<ISynchronizer> _logger;

        public RobustSynchronizer(ISynchronizer inner, ILogger<ISynchronizer> logger)
        {
            _inner = inner;
            _logger = logger;
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
#pragma warning disable CA1031 // Do not catch general exception types -- diagnostics path
            catch (Exception exception)
            {
                _logger.LogError(exception, "Synchronization failed.");
                await Task.Delay(15000, cancellationToken);
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }
    }
}
