using System;
using System.Threading;

using Microsoft.Extensions.Logging;

namespace Synchronizzer.Composition
{
    internal sealed class TracingJobFactory : ISynchronizationJobFactory
    {
        private readonly ISynchronizationJobFactory _inner;
        private readonly ILogger<TracingJobFactory> _logger;

        public TracingJobFactory(ISynchronizationJobFactory inner, ILogger<TracingJobFactory> logger)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public SynchronizationJob Create(SyncInfo info, CancellationToken cancellationToken)
        {
            try
            {
                return _inner.Create(info, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to create job \"{JobName}\".", info?.Name);
                throw;
            }
        }
    }
}
