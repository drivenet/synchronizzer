using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace GridFSSyncService.Implementation
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
            using (_logger.BeginScope("deleting \"{ObjectName}\"", objectName))
            {
                _logger.LogDebug(Events.BeginDelete, "Begin delete.");
                try
                {
                    await _inner.Delete(objectName, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation(Events.CancelledDelete, "Delete was cancelled.");
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Delete failed.");
                    throw;
                }

                _logger.LogDebug(Events.EndDelete, "End delete.");
            }
        }

        public Task Flush(CancellationToken cancellationToken)
        {
            return _inner.Flush(cancellationToken);
        }

        public async Task Upload(string objectName, Stream readOnlyInput, CancellationToken cancellationToken)
        {
            using (_logger.BeginScope("uploading \"{ObjectName}\"", objectName))
            {
                _logger.LogDebug(Events.BeginUpload, "Begin upload.");
                try
                {
                    await _inner.Upload(objectName, readOnlyInput, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation(Events.CancelledUpload, "Upload was cancelled.");
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Upload failed.");
                    throw;
                }

                _logger.LogDebug(Events.EndUpload, "End upload.");
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
