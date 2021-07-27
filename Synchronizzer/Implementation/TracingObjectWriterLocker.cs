using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Synchronizzer.Implementation
{
    internal sealed class TracingObjectWriterLocker : IObjectWriterLocker
    {
        private readonly IObjectWriterLocker _inner;
        private readonly ILogger _logger;

        public TracingObjectWriterLocker(IObjectWriterLocker inner, ILogger<TracingObjectWriterLocker> logger)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task Clear(CancellationToken cancellationToken) => _inner.Clear(cancellationToken);

        public async Task Lock(CancellationToken cancellationToken)
        {
            _logger.LogInformation(Events.Locking, "Locking with {Locker}.", _inner);
            try
            {
                await _inner.Lock(cancellationToken);
            }
            catch (OperationCanceledException exception)
            {
                _logger.LogInformation(
                    Events.LockCanceled,
                    exception,
                    "Locking with {Locker} was canceled (direct: {IsDirect}).",
                    _inner,
                    cancellationToken.IsCancellationRequested);
                throw;
            }

            _logger.LogInformation(Events.Locked, "Locked with {Locker}.", _inner);
        }

        private static class Events
        {
            public static readonly EventId Locking = new(1, nameof(Locking));
            public static readonly EventId Locked = new(2, nameof(Locked));
            public static readonly EventId LockCanceled = new(3, nameof(LockCanceled));
        }
    }
}
