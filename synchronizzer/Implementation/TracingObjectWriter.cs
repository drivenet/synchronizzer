using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Synchronizzer.Implementation
{
    internal sealed class TracingObjectWriter : IObjectWriter
    {
        private readonly IObjectWriter _inner;
        private readonly ILogger _logger;

        public TracingObjectWriter(IObjectWriter inner, ILogger<TracingObjectWriter> logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public async Task Delete(string objectName, CancellationToken cancellationToken)
        {
            using (_logger.BeginScope("delete \"{ObjectName}\"", objectName))
            {
                _logger.LogInformation(Events.BeginDelete, "Begin delete \"{ObjectName}\".", objectName);
                try
                {
                    await _inner.Delete(objectName, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning(Events.CancelledDelete, "Delete of \"{ObjectName}\" was cancelled.", objectName);
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Delete of \"{ObjectName}\" failed.", objectName);
                    throw;
                }

                _logger.LogDebug(Events.EndDelete, "End delete \"{ObjectName}\".", objectName);
            }
        }

        public Task Flush(CancellationToken cancellationToken) => _inner.Flush(cancellationToken);

        public async Task Upload(string objectName, Stream readOnlyInput, CancellationToken cancellationToken)
        {
            using (_logger.BeginScope("upload \"{ObjectName}\"", objectName))
            {
                _logger.LogInformation(Events.BeginUpload, "Begin upload \"{ObjectName}\".", objectName);
                try
                {
                    await _inner.Upload(objectName, readOnlyInput, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning(Events.CancelledUpload, "Upload of \"{ObjectName}\" was cancelled.", objectName);
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Upload of \"{ObjectName}\" failed.", objectName);
                    throw;
                }

                _logger.LogInformation(Events.EndUpload, "End upload \"{ObjectName}\".", objectName);
            }
        }

        private static class Events
        {
            public static readonly EventId BeginUpload = new EventId(1, nameof(BeginUpload));
            public static readonly EventId EndUpload = new EventId(2, nameof(EndUpload));
            public static readonly EventId BeginDelete = new EventId(3, nameof(BeginDelete));
            public static readonly EventId EndDelete = new EventId(4, nameof(EndDelete));
            public static readonly EventId CancelledUpload = new EventId(5, nameof(CancelledUpload));
            public static readonly EventId CancelledDelete = new EventId(6, nameof(CancelledDelete));
        }
    }
}
