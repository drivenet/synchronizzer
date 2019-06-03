using System;
using System.Threading;
using System.Threading.Tasks;

using GridFSSyncService.Components;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GridFSSyncService.Implementation
{
    internal sealed class SyncService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(21);
        private static readonly TimeSpan MinimumInterval = TimeSpan.FromMinutes(1);

        private readonly ISynchronizer _synchronizer;
        private readonly ITimeSource _timeSource;
        private readonly ILogger<SyncService> _logger;

        public SyncService(ISynchronizer synchronizer, ITimeSource timeSource, ILogger<SyncService> logger)
        {
            _synchronizer = synchronizer;
            _timeSource = timeSource;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (true)
            {
                var startTime = _timeSource.Now;
                await _synchronizer.Synchronize(stoppingToken);
                var timeSpent = _timeSource.Now - startTime;
                if (timeSpent < TimeSpan.Zero)
                {
                    _logger.LogCritical("Invalid time spent synchronizing {TimeSpent}, started at {StartTime}.", timeSpent, startTime);
                }

                var remaining = Interval - timeSpent;
                if (remaining < MinimumInterval)
                {
                    _logger.LogError("Remaining interval {Remaining} is less than minimum {MinimumInterval} (started at {StartTime}).", remaining, MinimumInterval, startTime);
                    remaining = MinimumInterval;
                }

                await Task.Delay(remaining, stoppingToken);
            }
        }
    }
}
