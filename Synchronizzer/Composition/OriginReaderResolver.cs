using System;

using Microsoft.Extensions.Logging;
using Microsoft.IO;

using Synchronizzer.Components;
using Synchronizzer.Implementation;

namespace Synchronizzer.Composition
{
    internal sealed class OriginReaderResolver : IOriginReaderResolver
    {
        private readonly IMetricsWriter _metricsWriter;
        private readonly ILogger<TracingObjectSource> _objectSourceLogger;
        private readonly ILogger<TracingObjectReader> _objectReaderLogger;
        private readonly RecyclableMemoryStreamManager _streamManager;

        public OriginReaderResolver(
            IMetricsWriter metricsWriter,
            ILogger<TracingObjectSource> objectSourceLogger,
            ILogger<TracingObjectReader> objectReaderLogger,
            RecyclableMemoryStreamManager streamManager)
        {
            _metricsWriter = metricsWriter ?? throw new ArgumentNullException(nameof(metricsWriter));
            _objectSourceLogger = objectSourceLogger ?? throw new ArgumentNullException(nameof(objectSourceLogger));
            _objectReaderLogger = objectReaderLogger ?? throw new ArgumentNullException(nameof(objectReaderLogger));
            _streamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        }

        public IOriginReader Resolve(string address)
        {
            if (Uri.TryCreate(address, UriKind.Absolute, out var uri))
            {
                if (uri.Scheme.Equals("s3", StringComparison.OrdinalIgnoreCase))
                {
                    return CreateS3Reader(uri);
                }

                return CreateFilesystemReader(uri);
            }

            if (address.StartsWith("mongodb://", StringComparison.OrdinalIgnoreCase))
            {
                return CreateGridFSReader(address);
            }

            throw new ArgumentOutOfRangeException(nameof(address), "Invalid origin address.");
        }

        private static IObjectReader Robust(IObjectReader reader)
            => new RobustObjectReader(reader);

        private static IObjectReader Retry(IObjectReader reader, byte retries)
            => new RetryingObjectReader(reader, retries);

        private static IObjectSource Retry(IObjectSource source, byte retries)
            => new RetryingObjectSource(source, retries);

        private IObjectReader Count(IObjectReader reader, string key)
            => new CountingObjectReader(reader, _metricsWriter, key);

        private IObjectSource Count(IObjectSource source, string key)
            => new CountingObjectSource(source, _metricsWriter, key);

        private IObjectReader Trace(IObjectReader reader)
            => new TracingObjectReader(reader, _objectReaderLogger);

        private IObjectSource Trace(IObjectSource source)
            => new TracingObjectSource(source, "origin", _objectSourceLogger);

        private IOriginReader CreateFilesystemReader(Uri uri)
        {
            var context = FilesystemUtils.CreateContext(uri);
            return new OriginReader(
                Trace(
                    Count(
                        new FilesystemObjectSource(context),
                        "origin.fs")),
                Robust(
                    Trace(
                        Count(
                            new FilesystemObjectReader(context),
                            "fs"))));
        }

        private IOriginReader CreateS3Reader(Uri uri)
        {
            var context = S3Utils.CreateContext(uri);
            return new OriginReader(
                Trace(
                    Count(
                        new S3ObjectSource(context),
                        "origin.s3")),
                Robust(
                    Trace(
                        Count(
                            new S3ObjectReader(context),
                            "s3"))));
        }

        private IOriginReader CreateGridFSReader(string address)
        {
            const byte GridFSRetries = 10;
            var context = GridFSUtils.CreateContext(address);
            return new OriginReader(
                Retry(
                    Trace(
                        Count(
                            new GridFSObjectSource(context),
                            "origin.gridfs")),
                    GridFSRetries),
                Robust(
                    Trace(
                        Count(
                            Retry(
                                new GridFSFilteringObjectReader(
                                    new BufferingObjectReader(
                                        new GridFSObjectReader(context),
                                        _streamManager)),
                                GridFSRetries),
                            "gridfs"))));
        }
    }
}
