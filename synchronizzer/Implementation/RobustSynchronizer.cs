using System;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class RobustSynchronizer : ISynchronizer
    {
        private readonly ISynchronizer _inner;

        public RobustSynchronizer(ISynchronizer inner)
        {
            _inner = inner;
        }

        public async Task Synchronize(CancellationToken cancellationToken)
        {
            try
            {
                await _inner.Synchronize(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
#pragma warning disable CA1031 // Do not catch general exception types -- required for robust operation
            catch (Exception exception)
            {
                Console.Error.WriteLine("Unexpected RobustSynchronizer.Synchronize exception:\n" + exception);
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }
    }
}
