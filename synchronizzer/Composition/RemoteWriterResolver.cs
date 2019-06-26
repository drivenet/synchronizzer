using System;

using Microsoft.Extensions.Logging;

using Synchronizzer.Components;
using Synchronizzer.Implementation;

namespace Synchronizzer.Composition
{
    internal sealed class RemoteWriterResolver : IRemoteWriterResolver
    {
        private readonly IQueuingTaskManagerSelector _taskManagerSelector;
        private readonly IMetricsWriter _metricsWriter;
        private readonly ILogger<TracingObjectWriter> _objectLogger;
        private readonly ILogger<TracingObjectSource> _objectSourceLogger;

        public RemoteWriterResolver(
            IQueuingTaskManagerSelector taskManagerSelector,
            IMetricsWriter metricsWriter,
            ILogger<TracingObjectWriter> objectLogger,
            ILogger<TracingObjectSource> objectSourceLogger)
        {
            _taskManagerSelector = taskManagerSelector;
            _metricsWriter = metricsWriter;
            _objectLogger = objectLogger;
            _objectSourceLogger = objectSourceLogger;
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
                    new TracingObjectSource(
                        new S3ObjectSource(context),
                        "remote",
                        _objectSourceLogger),
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
                    taskManager),
                new LockingObjectWriterLocker(
                    new S3ObjectWriterLocker(context)));
        }
    }
}
