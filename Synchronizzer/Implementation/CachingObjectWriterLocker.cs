using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class CachingObjectWriterLocker : IObjectWriterLocker
    {
        private static readonly TimeSpan LockInterval = TimeSpan.FromSeconds(47);

        private readonly Stopwatch _timer = new();
        private readonly IObjectWriterLocker _inner;

        public CachingObjectWriterLocker(IObjectWriterLocker inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public async Task Lock(CancellationToken cancellationToken)
        {
            if (!_timer.IsRunning
                || _timer.Elapsed > LockInterval)
            {
                await _inner.Lock(cancellationToken);
                _timer.Restart();
            }
        }

        public Task Clear(CancellationToken cancellationToken) => _inner.Clear(cancellationToken);
    }
}
