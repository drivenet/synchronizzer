using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

namespace GridFSSyncService.Implementation
{
    internal sealed class SyncService : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("start");
            await Task.Delay(2000, cancellationToken);
            Console.WriteLine("started");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("stop");
            await Task.Delay(2000, cancellationToken);
            Console.WriteLine("stopped");
        }
    }
}
