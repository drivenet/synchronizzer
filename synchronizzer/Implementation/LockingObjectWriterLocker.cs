using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
#pragma warning disable CA1001 // Types that own disposable fields should be disposable -- _lock does not need disposal because wait handle is never allocated
    internal sealed class LockingObjectWriterLocker : IObjectWriterLocker
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        private static readonly TimeSpan LockInterval = TimeSpan.FromSeconds(47);

        private readonly Stopwatch _timer = new Stopwatch();
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);
        private readonly IObjectWriterLocker _inner;

        public LockingObjectWriterLocker(IObjectWriterLocker inner)
        {
            _inner = inner;
        }

        public async Task Lock(CancellationToken cancellationToken)
        {
            try
            {
                await _lock.WaitAsync(cancellationToken);
                if (!_timer.IsRunning
                    || _timer.Elapsed > LockInterval)
                {
                    await _inner.Lock(cancellationToken);
                    _timer.Restart();
                }
            }
            finally
            {
                try
                {
                    _lock.Release();
                }
                catch (SemaphoreFullException)
                {
                }
            }
        }

        public Task Clear(CancellationToken cancellationToken) => _inner.Clear(cancellationToken);
    }
}
