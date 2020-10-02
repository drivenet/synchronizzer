using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class TimedSynchronizer : ISynchronizer
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(21);

        private readonly ISynchronizer _inner;
        private readonly SyncTimeHolder _timeHolder;

        public TimedSynchronizer(ISynchronizer inner, SyncTimeHolder timeHolder)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _timeHolder = timeHolder ?? throw new ArgumentNullException(nameof(timeHolder));
        }

        public async Task Synchronize(CancellationToken cancellationToken)
        {
            await _timeHolder.Wait(cancellationToken);
            var timer = Stopwatch.StartNew();
            try
            {
                await _inner.Synchronize(cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                UpdateInterval(timer.Elapsed);
                throw;
            }

            UpdateInterval(timer.Elapsed);
        }

        private void UpdateInterval(TimeSpan elapsed)
            => _timeHolder.SetWait(Interval - elapsed);
    }
}
