using System;
using System.Threading;
using System.Threading.Tasks;

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

        public async Task<ObjectsBatch> GetOrdered(string? continuationToken, CancellationToken cancellationToken)
        {
            ObjectsBatch result;
            try
            {
                result = await _inner.GetOrdered(continuationToken, cancellationToken);
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

            _writer.Add(_prefix + "gets", 1);
            _writer.Add(_prefix + "objects", result.Count);
            return result;
        }
    }
}
