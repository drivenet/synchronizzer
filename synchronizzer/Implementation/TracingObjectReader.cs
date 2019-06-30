using System;
using System.Diagnostics;
using System.IO;
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
            _inner = inner;
            _logger = logger;
        }

        public async Task<Stream?> Read(string objectName, CancellationToken cancellationToken)
        {
            Stream? stream;
            using (_logger.BeginScope("read \"{ObjectName}\"", objectName))
            {
                var timer = Stopwatch.StartNew();
                _logger.LogDebug(Events.Reading, "Reading \"{ObjectName}\".", objectName);
                try
                {
                    stream = await _inner.Read(objectName, cancellationToken);
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

                long? length;
                try
                {
                    length = stream?.Length;
                }
#pragma warning disable CA1031 // Do not catch general exception types -- required for robust diagnostics
                catch
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    length = null;
                }

                _logger.LogInformation(Events.Read, "Read \"{ObjectName}\", elapsed {Elapsed}, length {Length}.", objectName, timer.Elapsed.TotalMilliseconds, length);
            }

            return stream;
        }

        private static class Events
        {
            public static readonly EventId Reading = new EventId(1, nameof(Reading));
            public static readonly EventId Read = new EventId(2, nameof(Read));
            public static readonly EventId ReadCanceled = new EventId(3, nameof(ReadCanceled));
        }
    }
}
