using System.Threading;

namespace Synchronizzer.Composition
{
    internal sealed class SynchronizationJobFactory : ISynchronizationJobFactory
    {
        private readonly ISynchronizerFactory _factory;

        public SynchronizationJobFactory(ISynchronizerFactory factory)
        {
            _factory = factory ?? throw new System.ArgumentNullException(nameof(factory));
        }

        public SynchronizationJob Create(SyncInfo info, CancellationToken cancellationToken)
        {
            var synchronizer = _factory.Create(info);
            var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try
            {
                var task = synchronizer.Synchronize(cancel.Token);
                return new SynchronizationJob(task, cancel, synchronizer);
            }
            catch
            {
                cancel.Dispose();
                synchronizer.Dispose();
                throw;
            }
        }
    }
}
