using System;

using Microsoft.Extensions.Logging;

using Synchronizzer.Components;
using Synchronizzer.Implementation;

namespace Synchronizzer.Composition
{
    internal sealed class LocalReaderResolver : ILocalReaderResolver
    {
        private readonly IMetricsWriter _metricsWriter;
        private readonly ILogger<TracingObjectSource> _objectSourceLogger;
        private readonly ILogger<TracingObjectReader> _objectReaderLogger;

        public LocalReaderResolver(
            IMetricsWriter metricsWriter,
            ILogger<TracingObjectSource> objectSourceLogger,
            ILogger<TracingObjectReader> objectReaderLogger)
        {
            _metricsWriter = metricsWriter;
            _objectSourceLogger = objectSourceLogger;
            _objectReaderLogger = objectReaderLogger;
        }

        public ILocalReader Resolve(string address)
        {
            if (Uri.TryCreate(address, UriKind.Absolute, out var uri))
            {
                var context = FilesystemUtils.CreateContext(uri);
                return new LocalReader(
                    Count(
                        Trace(
                            new FilesystemObjectSource(context)),
                        "local.fs"),
                    Count(
                        Robust(
                            Trace(
                                new FilesystemObjectReader(context))),
                        "fs"));
            }

            if (address.StartsWith("mongodb://", StringComparison.OrdinalIgnoreCase))
            {
                const int Retries = 4;
                var context = GridFSUtils.CreateContext(address);
                return new LocalReader(
                    Count(
                        new RetryingObjectSource(
                            Trace(
                                new GridFSObjectSource(context)),
                            Retries),
                        "local.gridfs"),
                    Count(
                        Robust(
                            new RetryingObjectReader(
                                Trace(
                                    new BufferingObjectReader(
                                        new GridFSObjectReader(context))),
                                Retries)),
                        "gridfs"));
            }

            throw new ArgumentOutOfRangeException(nameof(address), "Invalid local address.");
        }

        private IObjectReader Count(IObjectReader reader, string key)
            => new CountingObjectReader(reader, _metricsWriter, key);

        private IObjectSource Count(IObjectSource source, string key)
            => new CountingObjectSource(source, _metricsWriter, key);

        private IObjectReader Robust(IObjectReader reader)
            => new RobustObjectReader(reader);

        private IObjectReader Trace(IObjectReader reader)
            => new TracingObjectReader(reader, _objectReaderLogger);

        private IObjectSource Trace(IObjectSource source)
            => new TracingObjectSource(source, "local", _objectSourceLogger);
    }
}
