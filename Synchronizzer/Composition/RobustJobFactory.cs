using System;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Composition
{
    internal sealed class RobustJobFactory : ISynchronizationJobFactory
    {
        private static readonly TimeSpan DelayBetweenErrors = TimeSpan.FromSeconds(45);

        private readonly ISynchronizationJobFactory _inner;

        public RobustJobFactory(ISynchronizationJobFactory inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public SynchronizationJob Create(SyncInfo info, CancellationToken cancellationToken)
        {
            try
            {
                return _inner.Create(info, cancellationToken);
            }
#pragma warning disable CA1031 // Do not catch general exception types -- required for robustness
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                try
                {
                    return new SynchronizationJob(Task.Delay(DelayBetweenErrors, cancellationToken), cancel);
                }
                catch
                {
                    cancel.Dispose();
                    throw;
                }
            }
        }
    }
}
