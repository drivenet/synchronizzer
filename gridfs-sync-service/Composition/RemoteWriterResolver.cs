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

        public IRemoteWriter Resolve(string address, string? recycleAddress)
        {
            if (!Uri.TryCreate(address, UriKind.Absolute, out var uri))
            {
                throw new ArgumentOutOfRangeException(nameof(address), "Invalid remote address.");
            }

            var context = S3Utils.CreateContext(uri);
            S3WriteContext? recycleContext;
            if (recycleAddress is object)
            {
                if (!Uri.TryCreate(recycleAddress, UriKind.Absolute, out var recycleUri))
                {
                    throw new ArgumentOutOfRangeException(nameof(address), "Invalid remote recycle address.");
                }

                recycleContext = S3Utils.CreateContext(recycleUri);
            }
            else
            {
                recycleContext = null;
            }

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
                                new S3ObjectWriter(context, recycleContext),
                                _metricsWriter,
                                "s3"),
                            _objectLogger)),
                    taskManager));
        }
    }
}
