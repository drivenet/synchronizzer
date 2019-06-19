using System.Threading;

namespace MongoDB.Driver
{
    public sealed class IteratorCancel
    {
        private readonly CancellationTokenSource _cancel;

        public IteratorCancel(CancellationTokenSource cancel)
        {
            _cancel = cancel;
        }

        public void Cancel()
        {
            _cancel.Cancel();
            _cancel.Token.ThrowIfCancellationRequested();
        }
    }
}
