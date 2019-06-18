using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Synchronizzer.Implementation;

namespace Synchronizzer.Composition
{
    internal sealed class JobManagingSynchronizer : ISynchronizer, IDisposable
    {
        private readonly ConcurrentDictionary<SyncInfo, Lazy<SynchronizationJob>> _jobs = new ConcurrentDictionary<SyncInfo, Lazy<SynchronizationJob>>();
        private readonly IEnumerable<SyncInfo> _infos;
        private readonly ISynchronizationJobFactory _jobFactory;

        public JobManagingSynchronizer(IEnumerable<SyncInfo> infos, ISynchronizationJobFactory jobFactory)
        {
            _infos = infos;
            _jobFactory = jobFactory;
        }

        public void Dispose()
        {
            foreach (var pair in _jobs)
            {
                pair.Value.Value.Dispose();
            }
        }

        public async Task Synchronize(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var infoSet = _infos.ToHashSet();
            foreach (var pair in _jobs)
            {
                if (pair.Value.Value.IsCompleted)
                {
                    await CompleteJob(pair.Key, cancel: false);
                }
                else if (!infoSet.Contains(pair.Key))
                {
                    await CompleteJob(pair.Key, cancel: true);
                }
            }

            await Task.Delay(337, cancellationToken);

            foreach (var info in infoSet)
            {
                _jobs.GetOrAdd(info, TaskFactory);
            }

            Lazy<SynchronizationJob> TaskFactory(SyncInfo info)
                => new Lazy<SynchronizationJob>(() => _jobFactory.Create(info, cancellationToken));
        }

        private async Task CompleteJob(SyncInfo info, bool cancel)
        {
            if (!_jobs.TryRemove(info, out var jobContainer))
            {
                return;
            }

            using var job = jobContainer.Value;
            if (cancel)
            {
                job.Cancel();
            }

            try
            {
                await job;
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
