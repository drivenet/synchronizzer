using System;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver
{
    public static class IteratorExtensions
    {
        public static async Task ForEachAsync<T>(this IAsyncCursor<T> cursor, Action<T, IteratorCancel> action, CancellationToken cancellationToken)
        {
            using var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var iteratorCancel = new IteratorCancel(cancel);
            try
            {
                await cursor.ForEachAsync(v => action(v, iteratorCancel), cancellationToken);
            }
            catch (OperationCanceledException exception) when (exception.CancellationToken == cancel.Token)
            {
            }
        }
    }
}
