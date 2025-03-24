using System;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class TimedSynchronizer : ISynchronizer
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(21);

        private readonly ISynchronizer _inner;
        private readonly SyncTimeHolder _timeHolder;
        private readonly TimeProvider _timeProvider;

        public TimedSynchronizer(ISynchronizer inner, SyncTimeHolder timeHolder, TimeProvider timeProvider)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _timeHolder = timeHolder ?? throw new ArgumentNullException(nameof(timeHolder));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public async Task Synchronize(CancellationToken cancellationToken)
        {
            await _timeHolder.Wait(_timeProvider, cancellationToken);
            var startedAt = _timeProvider.GetTimestamp();
            try
            {
                await _inner.Synchronize(cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                UpdateInterval(startedAt);
                throw;
            }

            UpdateInterval(startedAt);
        }

        public void Dispose() => _inner.Dispose();

        private void UpdateInterval(long startedAt)
            => _timeHolder.SetWait(Interval - _timeProvider.GetElapsedTime(startedAt));
    }
}
