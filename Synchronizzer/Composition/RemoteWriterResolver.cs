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
            _metricsWriter = metricsWriter;
            _objectLogger = objectLogger;
            _objectSourceLogger = objectSourceLogger;
            _lockerLogger = lockerLogger;
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

            var remoteAddress = context.S3.ServiceUrl.AbsoluteUri;
            var lockName = FormattableString.Invariant($"{Environment.MachineName.ToUpperInvariant()}/{Process.GetCurrentProcess().Id}/{Guid.NewGuid():N}");
            const byte S3Retries = 30;
            return new RemoteWriter(
                remoteAddress,
                new RetryingObjectSource(
                    new TracingObjectSource(
                        new CountingObjectSource(
                            new S3ObjectSource(context),
                            _metricsWriter,
                            "remote.s3"),
                        "remote",
                        _objectSourceLogger),
                    S3Retries),
                new RobustObjectWriter(
                    new TracingObjectWriter(
                        new CountingObjectWriter(
                            new S3ObjectWriter(context, recycleContext),
                            _metricsWriter,
                            "s3"),
                        _objectLogger)),
                new LockingObjectWriterLocker(
                    new CachingObjectWriterLocker(
                        new RetryingObjectWriterLocker(
                            new TracingObjectWriterLocker(
                                new S3ObjectWriterLocker(context, lockName),
                                _lockerLogger),
                            S3Retries))));
        }
    }
}
