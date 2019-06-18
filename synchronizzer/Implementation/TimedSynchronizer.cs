using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class TimedSynchronizer : ISynchronizer
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(7);

        private readonly ISynchronizer _inner;
        private readonly SyncTimeHolder _timeHolder;

        public TimedSynchronizer(ISynchronizer inner, SyncTimeHolder timeHolder)
        {
            _inner = inner;
            _timeHolder = timeHolder;
        }

        public async Task Synchronize(CancellationToken cancellationToken)
        {
            await _timeHolder.Wait(cancellationToken);
            var timer = Stopwatch.StartNew();
            await _inner.Synchronize(cancellationToken);
            _timeHolder.SetWait(Interval - timer.Elapsed);
        }
    }
}
