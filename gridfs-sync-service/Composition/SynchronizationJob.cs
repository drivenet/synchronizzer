using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GridFSSyncService.Composition
{
    internal sealed class SynchronizationJob : IDisposable
    {
        private readonly Task _task;
        private readonly CancellationTokenSource _cancel;

        public SynchronizationJob(Task task, CancellationTokenSource cancel)
        {
            _task = task;
            _cancel = cancel;
        }

        public bool IsCompleted => _task.IsCompleted;

        public void Cancel()
        {
            try
            {
                _cancel.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        public TaskAwaiter GetAwaiter() => _task.GetAwaiter();

        public void Dispose() => _cancel.Dispose();
    }
}
