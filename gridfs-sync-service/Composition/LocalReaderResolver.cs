using System;

using GridFSSyncService.Components;
using GridFSSyncService.Implementation;

using Microsoft.Extensions.Logging;

namespace GridFSSyncService.Composition
{
    internal sealed class LocalReaderResolver : ILocalReaderResolver
    {
        private readonly IMetricsWriter _metricsWriter;
        private readonly ILogger<TracingObjectSource> _objectSourceLogger;

        public LocalReaderResolver(IMetricsWriter metricsWriter, ILogger<TracingObjectSource> objectSourceLogger)
        {
            _metricsWriter = metricsWriter;
            _objectSourceLogger = objectSourceLogger;
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
                        new FilesystemObjectReader(context),
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
                        new RetryingObjectReader(
                            new GridFSObjectReader(context),
                            Retries),
                        "gridfs"));
            }

            throw new ArgumentOutOfRangeException(nameof(address), "Invalid local address.");
        }

        private IObjectReader Count(IObjectReader reader, string key)
            => new CountingObjectReader(reader, _metricsWriter, key);

        private IObjectSource Count(IObjectSource source, string key)
            => new CountingObjectSource(source, _metricsWriter, key);

        private IObjectSource Trace(IObjectSource source)
            => new TracingObjectSource(source, "local", _objectSourceLogger);
    }
}
