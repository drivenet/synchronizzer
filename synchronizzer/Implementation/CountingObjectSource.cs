using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using GridFSSyncService.Components;

namespace GridFSSyncService.Implementation
{
    internal sealed class CountingObjectSource : IObjectSource
    {
        private readonly IObjectSource _inner;
        private readonly IMetricsWriter _writer;
        private readonly string _prefix;

        public CountingObjectSource(IObjectSource inner, IMetricsWriter writer, string key)
        {
            _inner = inner;
            _writer = writer;
            _prefix = "source.";
            if (key.Length != 0)
            {
                _prefix = _prefix + key + ".";
            }
        }

        public async Task<IReadOnlyCollection<ObjectInfo>> GetOrdered(string? fromName, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<ObjectInfo> result;
            try
            {
                result = await _inner.GetOrdered(fromName, cancellationToken);
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
