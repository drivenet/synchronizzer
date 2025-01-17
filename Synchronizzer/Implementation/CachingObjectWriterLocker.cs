using System;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class CachingObjectWriterLocker : IObjectWriterLocker
    {
        private static readonly TimeSpan LockInterval = TimeSpan.FromSeconds(47);

        private readonly IObjectWriterLocker _inner;
        private readonly TimeProvider _timeProvider;
        private long _startTime;

        public CachingObjectWriterLocker(IObjectWriterLocker inner, TimeProvider timeProvider)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public async Task Lock(CancellationToken cancellationToken)
        {
            if (_startTime == 0
                || _timeProvider.GetElapsedTime(_startTime) > LockInterval)
            {
                await _inner.Lock(cancellationToken);
                _startTime = _timeProvider.GetTimestamp();
            }
        }

        public Task Clear(CancellationToken cancellationToken) => _inner.Clear(cancellationToken);
    }
}
