using System;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class SyncTimeHolder
    {
        private static readonly TimeSpan MinimumInterval = TimeSpan.FromMinutes(7);
        private static readonly TimeSpan MaximumInterval = TimeSpan.FromDays(1);

        private TimeSpan _wait;

        public void SetWait(TimeSpan wait)
        {
            if (wait < MinimumInterval)
            {
                wait = MinimumInterval;
            }
            else if (wait > MaximumInterval)
            {
                wait = MaximumInterval;
            }

            _wait = wait;
        }

        public async Task Wait(TimeProvider timeProvider, CancellationToken cancellationToken)
        {
            var wait = _wait;
            if (wait == TimeSpan.Zero)
            {
                return;
            }

            var startedAt = timeProvider.GetTimestamp();
            try
            {
                await Task.Delay(wait, cancellationToken);
            }
            finally
            {
                SetWait(wait - timeProvider.GetElapsedTime(startedAt));
            }
        }
    }
}
