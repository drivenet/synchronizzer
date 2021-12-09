using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Synchronizzer.Implementation
{
    internal sealed class TracingObjectReader : IObjectReader
    {
        private readonly IObjectReader _inner;
        private readonly ILogger _logger;

        public TracingObjectReader(IObjectReader inner, ILogger<TracingObjectReader> logger)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                var timer = Stopwatch.StartNew();
                _logger.LogDebug(Events.Reading, "Reading \"{ObjectName}\".", objectName);
                try
                {
                    readObject = await _inner.Read(objectName, cancellationToken);
                }
                catch (OperationCanceledException exception)
                {
                    _logger.LogInformation(
                        Events.ReadCanceled,
                        exception,
                        "Read of \"{ObjectName}\" was canceled, elapsed {Elapsed} (direct: {IsDirect}).",
                        objectName,
                        timer.Elapsed.TotalMilliseconds,
                        cancellationToken.IsCancellationRequested);
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to read \"{ObjectName}\", elapsed {Elapsed}.", objectName, timer.Elapsed.TotalMilliseconds);
                    throw;
                }

                _logger.LogDebug(Events.Read, "Read \"{ObjectName}\", length {Length}, elapsed {Elapsed}.", objectName, timer.Elapsed.TotalMilliseconds, readObject?.Length);
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
