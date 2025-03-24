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
        private readonly ILogger<TracingS3Mediator> _s3MediatorLogger;
        private readonly RecyclableMemoryStreamManager _streamManager;
        private readonly TimeProvider _timeProvider;

        public OriginReaderResolver(
            IMetricsWriter metricsWriter,
            ILogger<TracingObjectSource> objectSourceLogger,
            ILogger<TracingObjectReader> objectReaderLogger,
            ILogger<TracingS3Mediator> s3MediatorLogger,
            RecyclableMemoryStreamManager streamManager,
            TimeProvider timeProvider)
        {
            _metricsWriter = metricsWriter ?? throw new ArgumentNullException(nameof(metricsWriter));
            _objectSourceLogger = objectSourceLogger ?? throw new ArgumentNullException(nameof(objectSourceLogger));
            _objectReaderLogger = objectReaderLogger ?? throw new ArgumentNullException(nameof(objectReaderLogger));
            _s3MediatorLogger = s3MediatorLogger ?? throw new ArgumentNullException(nameof(s3MediatorLogger));
            _streamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
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

        private IObjectReader Count(IObjectReader reader, string key)
            => new CountingObjectReader(reader, _metricsWriter, key);

        private IObjectSource Count(IObjectSource source, string key)
            => new CountingObjectSource(source, _metricsWriter, key);

        private IObjectReader Trace(IObjectReader reader)
            => new TracingObjectReader(reader, _objectReaderLogger, _timeProvider);

        private IObjectSource Trace(IObjectSource source)
            => new TracingObjectSource(source, "origin", _objectSourceLogger, _timeProvider);

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
                            "fs"))),
                null,
                "file://" + context.FilePath);
        }

        private IOriginReader CreateS3Reader(Uri uri)
        {
            var context = S3Utils.CreateContext(uri, _s3MediatorLogger, _timeProvider);
            try
            {
                return new OriginReader(
                    Trace(
                        Buffer(
                            Count(
                                new S3ObjectSource(context),
                                "origin.s3"))),
                    Robust(
                        Trace(
                            Count(
                                new S3ObjectReader(context),
                                "s3"))),
                    context,
                    "s3://" + context.S3.ServiceUrl.GetComponents(UriComponents.NormalizedHost | UriComponents.Path, UriFormat.UriEscaped) + context.BucketName);
            }
            catch
            {
                ((IDisposable)context).Dispose();
                throw;
            }
        }

        private IOriginReader CreateGridFSReader(string address)
        {
            var context = GridFSUtils.CreateContext(address);
            try
            {
                return new OriginReader(
                    Trace(
                        Count(
                            new GridFSObjectSource(context),
                            "origin.gridfs")),
                    Robust(
                        Trace(
                            Count(
                                new GridFSFilteringObjectReader(
                                    new BufferingObjectReader(
                                        new GridFSObjectReader(context),
                                        _streamManager)),
                                "gridfs"))),
                    context,
                    "gridfs://" + context.Bucket.Database.DatabaseNamespace.DatabaseName + "/" + context.Bucket.Options.BucketName);
            }
            catch
            {
                ((IDisposable)context).Dispose();
                throw;
            }
        }

        private static IObjectSource Buffer(IObjectSource source)
            => new BufferingObjectSource(source);
    }
}
