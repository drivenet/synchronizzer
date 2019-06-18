using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
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

        public async Task Wait(CancellationToken cancellationToken)
        {
            var wait = _wait;
            var timer = Stopwatch.StartNew();
            try
            {
                await Task.Delay(wait, cancellationToken);
            }
            finally
            {
                SetWait(wait - timer.Elapsed);
            }
        }
    }
}
