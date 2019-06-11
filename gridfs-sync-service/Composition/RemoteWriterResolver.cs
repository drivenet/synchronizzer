using System;

using GridFSSyncService.Components;
using GridFSSyncService.Implementation;

using Microsoft.Extensions.Logging;

namespace GridFSSyncService.Composition
{
    internal sealed class RemoteWriterResolver : IRemoteWriterResolver
    {
        private readonly ILogger<TracingObjectWriter> _objectLogger;
        private readonly IQueuingTaskManagerSelector _taskManagerSelector;
        private readonly IMetricsWriter _metricsWriter;

        public RemoteWriterResolver(
            ILogger<TracingObjectWriter> objectLogger,
            IQueuingTaskManagerSelector taskManagerSelector,
            IMetricsWriter metricsWriter)
        {
            _objectLogger = objectLogger;
            _taskManagerSelector = taskManagerSelector;
            _metricsWriter = metricsWriter;
        }

        public IRemoteWriter Resolve(string address)
        {
            if (Uri.TryCreate(address, UriKind.Absolute, out var uri))
            {
                var context = S3Utils.CreateContext(uri);
                var taskManager = _taskManagerSelector.Select("s3|" + context.S3.Config.DetermineServiceURL());
                return new RemoteWriter(
                    new CountingObjectSource(
                        new S3ObjectSource(context),
                        _metricsWriter,
                        "remote.s3"),
                    new QueuingObjectWriter(
                        new RobustObjectWriter(
                            new TracingObjectWriter(
                                new CountingObjectWriter(
                                    new S3ObjectWriter(context),
                                    _metricsWriter,
                                    "s3"),
                                _objectLogger)),
                        taskManager));
            }

            throw new ArgumentOutOfRangeException(nameof(address), "Invalid remote address.");
        }
    }
}
