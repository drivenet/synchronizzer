using System;

using GridFSSyncService.Components;
using GridFSSyncService.Implementation;

using Microsoft.Extensions.Logging;

namespace GridFSSyncService.Composition
{
    internal sealed class RemoteWriterResolver : IRemoteWriterResolver
    {
        private readonly ILogger<TracingObjectWriter> _objectLogger;
        private readonly IMetricsWriter _metricsWriter;

        public RemoteWriterResolver(ILogger<TracingObjectWriter> objectLogger, IMetricsWriter metricsWriter)
        {
            _objectLogger = objectLogger;
            _metricsWriter = metricsWriter;
        }

        public IRemoteWriter Resolve(string address)
        {
            if (Uri.TryCreate(address, UriKind.Absolute, out var uri))
            {
                var context = S3Utils.CreateContext(uri);
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
                                _objectLogger))));
            }

            throw new ArgumentOutOfRangeException(nameof(address), "Invalid remote address.");
        }
    }
}
