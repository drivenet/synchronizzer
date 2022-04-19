using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

namespace Synchronizzer.Implementation
{
    internal sealed class SyncService : BackgroundService
    {
        private readonly ISynchronizer _synchronizer;

        public SyncService(ISynchronizer synchronizer)
        {
            _synchronizer = synchronizer ?? throw new ArgumentNullException(nameof(synchronizer));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _synchronizer.Synchronize(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
