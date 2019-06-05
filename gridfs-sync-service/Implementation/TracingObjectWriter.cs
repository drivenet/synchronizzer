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
            _logger.LogDebug("Deleting \"{ObjectName}\".", objectName);
            await _inner.Delete(objectName, cancellationToken);
            _logger.LogDebug("Deleted \"{ObjectName}\".", objectName);
        }

        public Task Flush(CancellationToken cancellationToken)
        {
            return _inner.Flush(cancellationToken);
        }

        public async Task Upload(string objectName, Stream readOnlyInput, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Uploading \"{ObjectName}\".", objectName);
            await _inner.Upload(objectName, readOnlyInput, cancellationToken);
            _logger.LogDebug("Uploaded \"{ObjectName}\".", objectName);
        }
    }
}
