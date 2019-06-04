using System.Threading;
using System.Threading.Tasks;

using GridFSSyncService.Implementation;

using Microsoft.Extensions.DependencyInjection;

namespace GridFSSyncService.Composition
{
    internal sealed class ScopingSynchronizer : ISynchronizer
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ScopingSynchronizer(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task Synchronize(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (var scope = _scopeFactory.CreateScope())
            {
                var synchronizer = scope.ServiceProvider.GetRequiredService<ISynchronizer>();
                await synchronizer.Synchronize(cancellationToken);
            }
        }
    }
}
