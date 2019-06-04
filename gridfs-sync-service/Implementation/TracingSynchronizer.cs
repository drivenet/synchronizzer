using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace GridFSSyncService.Implementation
{
    internal sealed class TracingSynchronizer : ISynchronizer
    {
        private readonly ISynchronizer _inner;
        private readonly ILogger<ISynchronizer> _logger;

        public TracingSynchronizer(ISynchronizer inner, ILogger<ISynchronizer> logger)
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
                _logger.LogWarning("Synchronization canceled.");
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Synchronization failed.");
                throw;
            }
        }
    }
}
