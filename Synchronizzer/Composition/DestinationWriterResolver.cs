using System;

using Microsoft.Extensions.Logging;

using Synchronizzer.Components;
using Synchronizzer.Implementation;

namespace Synchronizzer.Composition
{
    internal sealed class DestinationWriterResolver : IDestinationWriterResolver
    {
        private readonly IMetricsWriter _metricsWriter;
        private readonly ILogger<TracingObjectWriter> _objectLogger;
        private readonly ILogger<TracingObjectSource> _objectSourceLogger;
        private readonly ILogger<TracingObjectWriterLocker> _lockerLogger;
        private readonly ILogger<TracingS3Mediator> _s3MediatorLogger;
        private readonly TimeProvider _timeProvider;
        private readonly bool _logOperations;

        public DestinationWriterResolver(
            IMetricsWriter metricsWriter,
            ILogger<TracingObjectWriter> objectLogger,
            ILogger<TracingObjectSource> objectSourceLogger,
            ILogger<TracingObjectWriterLocker> lockerLogger,
            ILogger<TracingS3Mediator> s3MediatorLogger,
            TimeProvider timeProvider)
        {
            _metricsWriter = metricsWriter ?? throw new ArgumentNullException(nameof(metricsWriter));
            _objectLogger = objectLogger ?? throw new ArgumentNullException(nameof(objectLogger));
            _objectSourceLogger = objectSourceLogger ?? throw new ArgumentNullException(nameof(objectSourceLogger));
            _lockerLogger = lockerLogger ?? throw new ArgumentNullException(nameof(lockerLogger));
            _s3MediatorLogger = s3MediatorLogger ?? throw new ArgumentNullException(nameof(s3MediatorLogger));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _logOperations = true;
        }

        public IDestinationWriter Resolve(string address, string? recycleAddress, bool dryRun)
        {
            if (!Uri.TryCreate(address, UriKind.Absolute, out var uri))
            {
                throw new ArgumentOutOfRangeException(nameof(address), "Invalid destination address.");
            }

            if (uri.Scheme.Equals("s3", StringComparison.OrdinalIgnoreCase))
            {
                return CreateS3Writer(recycleAddress, uri, dryRun);
            }

            return CreateFilesystemWriter(address, recycleAddress, uri, dryRun);
        }

        private static IObjectWriterLocker Lock(IObjectWriterLocker inner) => new LockingObjectWriterLocker(inner);

        private IObjectWriterLocker Cache(IObjectWriterLocker inner) => new CachingObjectWriterLocker(inner, _timeProvider);

        private static IObjectWriter Robust(IObjectWriter inner) => new RobustObjectWriter(inner);

        private IObjectWriterLocker Trace(IObjectWriterLocker inner) => new TracingObjectWriterLocker(inner, _lockerLogger);

        private IObjectWriter Count(IObjectWriter inner, string key) => new CountingObjectWriter(inner, _metricsWriter, key);

        private IObjectWriter Trace(IObjectWriter inner) => new TracingObjectWriter(inner, _logOperations, _objectLogger, _timeProvider);

        private IObjectSource Count(IObjectSource inner, string key) => new CountingObjectSource(inner, _metricsWriter, key);

        private IObjectSource Trace(IObjectSource inner, string source) => new TracingObjectSource(inner, source, _objectSourceLogger, _timeProvider);

        private IDestinationWriter CreateS3Writer(string? recycleAddress, Uri uri, bool dryRun)
        {
            IDisposable disposable;
            S3WriteContext? recycleContext = null;
            var context = S3Utils.CreateWriteContext(uri, _s3MediatorLogger, _timeProvider);
            try
            {
                if (recycleAddress is not null)
                {
                    if (!Uri.TryCreate(recycleAddress, UriKind.Absolute, out var recycleUri))
                    {
                        throw new ArgumentOutOfRangeException(nameof(recycleAddress), "Invalid S3 recycle address.");
                    }

#pragma warning disable CA2000 // Dispose objects before losing scope -- disposed by composite
                    recycleContext = S3Utils.CreateWriteContext(recycleUri, _s3MediatorLogger, _timeProvider);
#pragma warning restore CA2000 // Dispose objects before losing scope
                    disposable = new CompositeDisposable([context, recycleContext]);
                }
                else
                {
                    disposable = context;
                }

                var lockName = FormattableString.Invariant($"{Environment.MachineName.ToUpperInvariant()}_{Environment.ProcessId}_{Guid.NewGuid():N}");
                return new DestinationWriter(
                    "s3://" + context.S3.Prefix + context.BucketName,
                    Trace(
                        Buffer(
                            Count(
                                new S3ObjectSource(context),
                                "destination.s3")),
                        "destination"),
                    Robust(
                        Trace(
                            Count(
                                dryRun
                                    ? NullObjectWriter.Instance
                                    : new S3ObjectWriter(context, recycleContext),
                                "s3"))),
                    Lock(
                        Cache(
                            Trace(
                                new S3ObjectWriterLocker(context, lockName, _timeProvider)))),
                    disposable);
            }
            catch
            {
                (recycleContext as IDisposable)?.Dispose();
                ((IDisposable)context).Dispose();
                throw;
            }
        }

        private IDestinationWriter CreateFilesystemWriter(string address, string? recycleAddress, Uri uri, bool dryRun)
        {
            var context = FilesystemUtils.CreateContext(uri);
            FilesystemContext? recycleContext;
            if (recycleAddress is not null)
            {
                if (!Uri.TryCreate(recycleAddress, UriKind.Absolute, out var recycleUri))
                {
                    throw new ArgumentOutOfRangeException(nameof(address), "Invalid filesystem recycle address.");
                }

                recycleContext = FilesystemUtils.CreateContext(recycleUri);
            }
            else
            {
                recycleContext = null;
            }

            var lockName = FormattableString.Invariant($"{Environment.ProcessId}_{Guid.NewGuid():N}");
            return new DestinationWriter(
                address,
                Trace(
                    Count(
                        new FilesystemObjectSource(context),
                        "destination.fs"),
                    "destination"),
                Trace(
                    Count(
                        dryRun
                            ? NullObjectWriter.Instance
                            : new FilesystemObjectWriter(context, recycleContext),
                        "fs")),
                Lock(
                    Cache(
                        Trace(
                            new FilesystemObjectWriterLocker(context, lockName, _timeProvider)))),
                null);
        }

        private static IObjectSource Buffer(IObjectSource source)
            => new BufferingObjectSource(source);
    }
}
