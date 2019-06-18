using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Synchronizzer.Implementation
{
    internal sealed class TracingSynchronizer : ISynchronizer
    {
        private readonly ISynchronizer _inner;
        private readonly ILogger _logger;
        private readonly string _name;

        public TracingSynchronizer(ISynchronizer inner, ILogger<TracingSynchronizer> logger, string name)
        {
            _inner = inner;
            _logger = logger;
            _name = name;
        }

        public async Task Synchronize(CancellationToken cancellationToken)
        {
            using (_logger.BeginScope("synchronize \"{Name}\", session \"{Id:N}\"", _name, Guid.NewGuid()))
            {
                _logger.LogInformation(Events.Begin, "Begin synchronization.");
                try
                {
                    await _inner.Synchronize(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Synchronization was canceled.");
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Synchronization failed.");
                    throw;
                }

                _logger.LogInformation(Events.End, "End synchronization.");
            }
        }

        private static class Events
        {
            public static readonly EventId Begin = new EventId(1, nameof(Begin));
            public static readonly EventId End = new EventId(2, nameof(End));
        }
    }
}
