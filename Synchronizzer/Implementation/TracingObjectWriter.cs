using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Synchronizzer.Implementation
{
    internal sealed class TracingObjectWriter : IObjectWriter
    {
        private readonly IObjectWriter _inner;
        private readonly LogLevel _opLogLevel;
        private readonly ILogger _logger;
        private readonly TimeProvider _timeProvider;

        public TracingObjectWriter(IObjectWriter inner, bool logOperations, ILogger<TracingObjectWriter> logger, TimeProvider timeProvider)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _opLogLevel = logOperations ? LogLevel.Information : LogLevel.Debug;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public async Task Delete(string objectName, CancellationToken cancellationToken)
        {
            if (objectName is null)
            {
                throw new ArgumentNullException(nameof(objectName));
            }

            using (_logger.BeginScope("delete \"{ObjectName}\"", objectName))
            {
                var startTime = _timeProvider.GetTimestamp();
                _logger.LogDebug(Events.Delete, "Delete \"{ObjectName}\".", objectName);
                try
                {
                    await _inner.Delete(objectName, cancellationToken);
                }
                catch (OperationCanceledException exception)
                {
                    _logger.LogWarning(
                        Events.DeleteCanceled,
                        exception,
                        "Delete of \"{ObjectName}\" was canceled, elapsed {Elapsed} (direct: {IsDirect}).",
                        objectName,
                        _timeProvider.GetElapsedTime(startTime).TotalMilliseconds,
                        cancellationToken.IsCancellationRequested);
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to delete \"{ObjectName}\", elapsed {Elapsed}.", objectName, _timeProvider.GetElapsedTime(startTime).TotalMilliseconds);
                    throw;
                }

                _logger.Log(_opLogLevel, Events.Deleted, "Deleted \"{ObjectName}\", elapsed {Elapsed}.", objectName, _timeProvider.GetElapsedTime(startTime).TotalMilliseconds);
            }
        }

        public async Task Upload(string objectName, ReadObject readObject, CancellationToken cancellationToken)
        {
            if (objectName is null)
            {
                throw new ArgumentNullException(nameof(objectName));
            }

            if (readObject is null)
            {
                throw new ArgumentNullException(nameof(readObject));
            }

            using (_logger.BeginScope("upload \"{ObjectName}\"", objectName))
            {
                var startTime = _timeProvider.GetTimestamp();
                var objectLength = readObject.Length;
                _logger.LogDebug(Events.Upload, "Upload \"{ObjectName}\", length {ObjectLength}.", objectName, objectLength);
                try
                {
                    await _inner.Upload(objectName, readObject, cancellationToken);
                }
                catch (OperationCanceledException exception)
                {
                    _logger.LogWarning(
                        Events.UploadCanceled,
                        exception,
                        "Upload of \"{ObjectName}\" was canceled, elapsed {Elapsed} (direct: {IsDirect}).",
                        objectName,
                        _timeProvider.GetElapsedTime(startTime).TotalMilliseconds,
                        cancellationToken.IsCancellationRequested);
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to upload \"{ObjectName}\", length {ObjectLength}, elapsed {Elapsed}.", objectName, objectLength, _timeProvider.GetElapsedTime(startTime).TotalMilliseconds);
                    throw;
                }

                _logger.Log(_opLogLevel, Events.Uploaded, "Uploaded \"{ObjectName}\", length {ObjectLength}, elapsed {Elapsed}.", objectName, objectLength, _timeProvider.GetElapsedTime(startTime).TotalMilliseconds);
            }
        }

        private static class Events
        {
            public static readonly EventId Upload = new(1, nameof(Upload));
            public static readonly EventId Uploaded = new(2, nameof(Uploaded));
            public static readonly EventId Delete = new(3, nameof(Delete));
            public static readonly EventId Deleted = new(4, nameof(Deleted));
            public static readonly EventId UploadCanceled = new(5, nameof(UploadCanceled));
            public static readonly EventId DeleteCanceled = new(6, nameof(DeleteCanceled));
        }
    }
}
