using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal sealed class SyncTimeHolder
    {
        private TimeSpan _wait;

        public static SyncTimeHolder Instance { get; } = new SyncTimeHolder();

        public void SetWait(TimeSpan wait)
        {
            if (wait < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(wait), wait, "Negative wait.");
            }

            _wait = wait;
        }

        public async Task Wait(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var wait = _wait;
            if (wait == TimeSpan.Zero)
            {
                return;
            }

            var timer = Stopwatch.StartNew();
            try
            {
                await Task.Delay(wait, cancellationToken);
            }
            finally
            {
                wait -= timer.Elapsed;
                _wait = wait > TimeSpan.Zero ? wait : TimeSpan.Zero;
            }
        }
    }
}
