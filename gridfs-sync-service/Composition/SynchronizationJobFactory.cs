using System.Threading;

namespace GridFSSyncService.Composition
{
    internal sealed class SynchronizationJobFactory : ISynchronizationJobFactory
    {
        private readonly ISynchronizerFactory _factory;

        public SynchronizationJobFactory(ISynchronizerFactory factory)
        {
            _factory = factory;
        }

        public SynchronizationJob Create(SyncInfo info, CancellationToken cancellationToken)
        {
            var synchronizer = _factory.Create(info);
            var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try
            {
                var task = synchronizer.Synchronize(cancel.Token);
                return new SynchronizationJob(task, cancel);
            }
            catch
            {
                cancel.Dispose();
                throw;
            }
        }
    }
}
