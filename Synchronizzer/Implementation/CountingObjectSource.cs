using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using Synchronizzer.Components;

namespace Synchronizzer.Implementation
{
    internal sealed class CountingObjectSource : IObjectSource
    {
        private readonly IObjectSource _inner;
        private readonly IMetricsWriter _writer;
        private readonly string _prefix;

        public CountingObjectSource(IObjectSource inner, IMetricsWriter writer, string key)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _prefix = "source.";
            if (key.Length != 0)
            {
                _prefix = _prefix + key + ".";
            }
        }

        public async IAsyncEnumerable<IReadOnlyCollection<ObjectInfo>> GetOrdered([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            IAsyncEnumerator<IReadOnlyCollection<ObjectInfo>>? enumerator = null;
            try
            {
                while (true)
                {
                    try
                    {
                        enumerator ??= _inner.GetOrdered(cancellationToken).GetAsyncEnumerator(cancellationToken);
                        if (!await enumerator.MoveNextAsync())
                        {
                            break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch
                    {
                        _writer.Add(_prefix + "get_errors", 1);
                        throw;
                    }

                    var batch = enumerator.Current;
                    _writer.Add(_prefix + "gets", 1);
                    _writer.Add(_prefix + "objects", batch.Count);
                    yield return batch;
                }
            }
            finally
            {
                if (enumerator is not null)
                {
                    await enumerator.DisposeAsync();
                }
            }
        }
    }
}
