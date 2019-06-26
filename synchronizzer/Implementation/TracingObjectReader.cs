using System;
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
                _logger.LogDebug(Events.Reading, "Reading \"{ObjectName}\".", objectName);
                try
                {
                    stream = await _inner.Read(objectName, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation(Events.ReadCanceled, "Read of \"{ObjectName}\" was canceled.", objectName);
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to read \"{ObjectName}\".", objectName);
                    throw;
                }

                _logger.LogDebug(Events.Read, "Read \"{ObjectName}\", stream {Stream}.", objectName, stream);
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
