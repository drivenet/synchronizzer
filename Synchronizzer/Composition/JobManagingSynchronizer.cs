using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Synchronizzer.Implementation;

namespace Synchronizzer.Composition
{
    internal sealed class JobManagingSynchronizer : ISynchronizer, IDisposable
    {
        private readonly ConcurrentDictionary<SyncInfo, Lazy<SynchronizationJob>> _jobs = new();
        private readonly IEnumerable<SyncInfo> _infos;
        private readonly ISynchronizationJobFactory _jobFactory;

        public JobManagingSynchronizer(IEnumerable<SyncInfo> infos, ISynchronizationJobFactory jobFactory)
        {
            _infos = infos ?? throw new ArgumentNullException(nameof(infos));
            _jobFactory = jobFactory ?? throw new ArgumentNullException(nameof(jobFactory));
        }

        public void Dispose()
        {
            foreach (var (_, job) in _jobs)
            {
                if (job.IsValueCreated)
                {
                    job.Value.Dispose();
                }
            }
        }

        public async Task Synchronize(CancellationToken cancellationToken)
        {
            HashSet<SyncInfo> infos;
            try
            {
                var task = Task.Delay(997, cancellationToken);
                infos = _infos.ToHashSet();
                await task;
            }
            catch (OperationCanceledException)
            {
                infos = new HashSet<SyncInfo>();
            }

            foreach (var (info, job) in _jobs)
            {
                if (job.Value.IsCompleted)
                {
                    await CompleteJob(info, cancel: false);
                }
                else if (!infos.Contains(info))
                {
                    await CompleteJob(info, cancel: true);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            var destinations = new Dictionary<string, string>();
            foreach (var info in infos)
            {
                var destination = info.Destination;
                if (!destinations.TryAdd(destination, info.Name))
                {
                    throw new InvalidDataException(FormattableString.Invariant($"Destination for job \"{info.Name}\" was already used for job \"{destinations[destination]}\"."));
                }

                if (info.Recycle is { } recyle
                    && !destinations.TryAdd(recyle, info.Name))
                {
                    throw new InvalidDataException(FormattableString.Invariant($"Recycle for job \"{info.Name}\" was already used for job \"{destinations[recyle]}\"."));
                }
            }

            foreach (var info in infos)
            {
                _jobs.GetOrAdd(info, TaskFactory);
            }

            Lazy<SynchronizationJob> TaskFactory(SyncInfo info)
                => new(() => _jobFactory.Create(info, cancellationToken));
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
