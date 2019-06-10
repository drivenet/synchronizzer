using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GridFSSyncService.Implementation
{
    internal sealed class SyncService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(21);
        private static readonly TimeSpan MinimumInterval = TimeSpan.FromMinutes(1);

        private readonly ISynchronizer _synchronizer;
        private readonly SyncTimeHolder _timeHolder;
        private readonly ILogger _logger;

        public SyncService(ISynchronizer synchronizer, SyncTimeHolder timeHolder, ILogger<SyncService> logger)
        {
            _synchronizer = synchronizer;
            _logger = logger;
            _timeHolder = timeHolder;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (true)
            {
                await _timeHolder.Wait(stoppingToken);
                _timeHolder.SetWait(MinimumInterval);

                var timer = Stopwatch.StartNew();
                await _synchronizer.Synchronize(stoppingToken);
                var timeSpent = timer.Elapsed;
                if (timeSpent < TimeSpan.Zero)
                {
                    _logger.LogCritical("Invalid time spent synchronizing {TimeSpent}.", timeSpent);
                }

                var wait = Interval - timeSpent;
                if (wait < MinimumInterval)
                {
                    _logger.LogError("Wait {Wait} is less than minimum {MinimumInterval}.", wait, MinimumInterval);
                    wait = MinimumInterval;
                }

                _timeHolder.SetWait(wait);
            }
        }
    }
}
