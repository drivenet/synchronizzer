using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Synchronizzer.Implementation
{
    internal sealed class TracingObjectReader : IObjectReader
    {
        private readonly IObjectReader _inner;
        private readonly ILogger _logger;
        private readonly TimeProvider _timeProvider;

        public TracingObjectReader(IObjectReader inner, ILogger<TracingObjectReader> logger, TimeProvider timeProvider)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public async Task<ReadObject?> Read(string objectName, CancellationToken cancellationToken)
        {
            if (objectName is null)
            {
                throw new ArgumentNullException(nameof(objectName));
            }

            ReadObject? readObject;
            using (_logger.BeginScope("read \"{ObjectName}\"", objectName))
            {
                var startedAt = _timeProvider.GetTimestamp();
                _logger.LogDebug(Events.Reading, "Reading \"{ObjectName}\".", objectName);
                try
                {
                    readObject = await _inner.Read(objectName, cancellationToken);
                }
                catch (OperationCanceledException exception)
                {
                    _logger.LogWarning(
                        Events.ReadCanceled,
                        exception,
                        "Read of \"{ObjectName}\" was canceled, elapsed {Elapsed} (direct: {IsDirect}).",
                        objectName,
                        _timeProvider.GetElapsedTime(startedAt).TotalMilliseconds,
                        cancellationToken.IsCancellationRequested);
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to read \"{ObjectName}\", elapsed {Elapsed}.", objectName, _timeProvider.GetElapsedTime(startedAt).TotalMilliseconds);
                    throw;
                }

                _logger.LogDebug(Events.Read, "Read \"{ObjectName}\", length {Length}, elapsed {Elapsed}.", objectName, _timeProvider.GetElapsedTime(startedAt).TotalMilliseconds, readObject?.Length);
            }

            return readObject;
        }

        private static class Events
        {
            public static readonly EventId Reading = new(1, nameof(Reading));
            public static readonly EventId Read = new(2, nameof(Read));
            public static readonly EventId ReadCanceled = new(3, nameof(ReadCanceled));
        }
    }
}
