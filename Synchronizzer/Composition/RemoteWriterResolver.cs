using System;
using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Synchronizzer.Components;
using Synchronizzer.Implementation;

namespace Synchronizzer.Composition
{
    internal sealed class RemoteWriterResolver : IRemoteWriterResolver
    {
        private readonly IMetricsWriter _metricsWriter;
        private readonly ILogger<TracingObjectWriter> _objectLogger;
        private readonly ILogger<TracingObjectSource> _objectSourceLogger;
        private readonly ILogger<TracingObjectWriterLocker> _lockerLogger;

        public RemoteWriterResolver(
            IMetricsWriter metricsWriter,
            ILogger<TracingObjectWriter> objectLogger,
            ILogger<TracingObjectSource> objectSourceLogger,
            ILogger<TracingObjectWriterLocker> lockerLogger)
        {
            _metricsWriter = metricsWriter ?? throw new ArgumentNullException(nameof(metricsWriter));
            _objectLogger = objectLogger ?? throw new ArgumentNullException(nameof(objectLogger));
            _objectSourceLogger = objectSourceLogger ?? throw new ArgumentNullException(nameof(objectSourceLogger));
            _lockerLogger = lockerLogger ?? throw new ArgumentNullException(nameof(lockerLogger));
        }

        public IRemoteWriter Resolve(string address, string? recycleAddress)
        {
            if (!Uri.TryCreate(address, UriKind.Absolute, out var uri))
            {
                throw new ArgumentOutOfRangeException(nameof(address), "Invalid remote address.");
            }

            if (uri.Scheme.Equals("s3", StringComparison.OrdinalIgnoreCase))
            {
                return CreateS3Writer(address, recycleAddress, uri);
            }

            return CreateFilesystemWriter(address, recycleAddress, uri);
        }

        private static IObjectWriterLocker Lock(IObjectWriterLocker inner) => new LockingObjectWriterLocker(inner);

        private static IObjectWriterLocker Cache(IObjectWriterLocker inner) => new CachingObjectWriterLocker(inner);

        private static IObjectWriterLocker Retry(IObjectWriterLocker inner, byte retries) => new RetryingObjectWriterLocker(inner, retries);

        private static IObjectWriter Robust(IObjectWriter inner) => new RobustObjectWriter(inner);

        private static IObjectSource Retry(IObjectSource inner, byte retries) => new RetryingObjectSource(inner, retries);

        private IObjectWriterLocker Trace(IObjectWriterLocker inner) => new TracingObjectWriterLocker(inner, _lockerLogger);

        private IObjectWriter Count(IObjectWriter inner, string key) => new CountingObjectWriter(inner, _metricsWriter, key);

        private IObjectWriter Trace(IObjectWriter inner) => new TracingObjectWriter(inner, _objectLogger);

        private IObjectSource Count(IObjectSource inner, string key) => new CountingObjectSource(inner, _metricsWriter, key);

        private IObjectSource Trace(IObjectSource inner, string source) => new TracingObjectSource(inner, source, _objectSourceLogger);

        private IRemoteWriter CreateS3Writer(string address, string? recycleAddress, Uri uri)
        {
            var context = S3Utils.CreateWriteContext(uri);
            S3WriteContext? recycleContext;
            if (recycleAddress is object)
            {
                if (!Uri.TryCreate(recycleAddress, UriKind.Absolute, out var recycleUri))
                {
                    throw new ArgumentOutOfRangeException(nameof(address), "Invalid S3 recycle address.");
                }

                recycleContext = S3Utils.CreateWriteContext(recycleUri);
            }
            else
            {
                recycleContext = null;
            }

            var remoteAddress = context.S3.ServiceUrl.AbsoluteUri;
            var lockName = FormattableString.Invariant($"{Environment.MachineName.ToUpperInvariant()}_{Process.GetCurrentProcess().Id}_{Guid.NewGuid():N}");
            const byte S3Retries = 30;
            return new RemoteWriter(
                remoteAddress,
                Retry(
                    Trace(
                        Count(
                            new S3ObjectSource(context),
                            "remote.s3"),
                        "remote"),
                    S3Retries),
                Robust(
                    Trace(
                        Count(
                            new S3ObjectWriter(context, recycleContext),
                            "s3"))),
                Lock(
                    Cache(
                        Retry(
                            Trace(
                                new S3ObjectWriterLocker(context, lockName)),
                            S3Retries))));
        }

        private IRemoteWriter CreateFilesystemWriter(string address, string? recycleAddress, Uri uri)
        {
            var context = FilesystemUtils.CreateContext(uri);
            FilesystemContext? recycleContext;
            if (recycleAddress is object)
            {
                if (!Uri.TryCreate(recycleAddress, UriKind.Absolute, out var recycleUri))
                {
                    throw new ArgumentOutOfRangeException(nameof(address), "Invalid S3 recycle address.");
                }

                recycleContext = FilesystemUtils.CreateContext(recycleUri);
            }
            else
            {
                recycleContext = null;
            }

            const byte FilesystemRetries = 10;
            var lockName = FormattableString.Invariant($"{Process.GetCurrentProcess().Id}_{Guid.NewGuid():N}");
            return new RemoteWriter(
                address,
                Trace(
                    Count(
                        new FilesystemObjectSource(context),
                        "remote.fs"),
                    "remote"),
                Trace(
                    Count(
                        new FilesystemObjectWriter(context, recycleContext),
                        "fs")),
                Lock(
                    Cache(
                        Retry(
                            Trace(
                                new FilesystemObjectWriterLocker(context, lockName)),
                            FilesystemRetries))));
        }
    }
}
