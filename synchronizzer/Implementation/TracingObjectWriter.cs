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
                _logger.LogDebug(Events.Delete, "Delete \"{ObjectName}\".", objectName);
                try
                {
                    await _inner.Delete(objectName, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning(Events.DeleteCancelled, "Delete of \"{ObjectName}\" was cancelled.", objectName);
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Failed to delete \"{ObjectName}\".", objectName);
                    throw;
                }

                _logger.LogInformation(Events.Deleted, "Deleted \"{ObjectName}\".", objectName);
            }
        }

        public Task Flush(CancellationToken cancellationToken) => _inner.Flush(cancellationToken);

        public async Task Upload(string objectName, Stream readOnlyInput, CancellationToken cancellationToken)
        {
            using (_logger.BeginScope("upload \"{ObjectName}\"", objectName))
            {
                var objectLength = readOnlyInput.Length;
                _logger.LogDebug(Events.Upload, "Upload \"{ObjectName}\", length {ObjectLength}.", objectName, objectLength);
                try
                {
                    await _inner.Upload(objectName, readOnlyInput, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning(Events.UploadCancelled, "Upload of \"{ObjectName}\" was cancelled.", objectName);
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Failed to upload \"{ObjectName}\", length {ObjectLength}.", objectName, objectLength);
                    throw;
                }

                _logger.LogInformation(Events.Uploaded, "Uploaded \"{ObjectName}\", length {ObjectLength}.", objectName, objectLength);
            }
        }

        private static class Events
        {
            public static readonly EventId Upload = new EventId(1, nameof(Upload));
            public static readonly EventId Uploaded = new EventId(2, nameof(Uploaded));
            public static readonly EventId Delete = new EventId(3, nameof(Delete));
            public static readonly EventId Deleted = new EventId(4, nameof(Deleted));
            public static readonly EventId UploadCancelled = new EventId(5, nameof(UploadCancelled));
            public static readonly EventId DeleteCancelled = new EventId(6, nameof(DeleteCancelled));
        }
    }
}
