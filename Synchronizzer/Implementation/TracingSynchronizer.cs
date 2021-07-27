using System;
using System.Globalization;
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
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public async Task Synchronize(CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            using (_logger.BeginScope("synchronize \"{JobName}\", session \"{JobId}\"", _name, id))
            {
                _logger.LogInformation(Events.Begin, "Begin job \"{JobName}\", session \"{JobId}\".", _name, id);
                try
                {
                    await _inner.Synchronize(cancellationToken);
                }
                catch (OperationCanceledException exception)
                {
                    _logger.LogWarning(
                        "Job \"{JobName}\" was canceled, session \"{JobId}\" (direct: {IsDirect}): {Reason}",
                        _name,
                        id,
                        cancellationToken.IsCancellationRequested,
                        exception.Message);
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Job \"{JobName}\" failed, session \"{JobId}\".", _name, id);
                    throw;
                }

                _logger.LogInformation(Events.End, "End job \"{JobName}\", session \"{JobId}\".", _name, id);
            }
        }

        private static class Events
        {
            public static readonly EventId Begin = new(1, nameof(Begin));
            public static readonly EventId End = new(2, nameof(End));
        }
    }
}
