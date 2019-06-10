using System.IO;
using System.Threading;
using System.Threading.Tasks;

using GridFSSyncService.Components;

namespace GridFSSyncService.Implementation
{
    internal sealed class CountingObjectWriter : IObjectWriter
    {
        private readonly IObjectWriter _inner;
        private readonly IMetricsWriter _writer;

        public CountingObjectWriter(IObjectWriter inner, IMetricsWriter writer)
        {
            _inner = inner;
            _writer = writer;
        }

        public async Task Delete(string objectName, CancellationToken cancellationToken)
        {
            _writer.Add("writer.delete.init", 1);
            await _inner.Delete(objectName, cancellationToken);
            _writer.Add("writer.delete.count", 1);
        }

        public async Task Flush(CancellationToken cancellationToken)
        {
            _writer.Add("writer.flush.init", 1);
            await _inner.Flush(cancellationToken);
            _writer.Add("writer.flush.count", 1);
        }

        public async Task Upload(string objectName, Stream readOnlyInput, CancellationToken cancellationToken)
        {
            _writer.Add("writer.upload.init", 1);
            var length = readOnlyInput.Length;
            await _inner.Upload(objectName, readOnlyInput, cancellationToken);
            _writer.Add("writer.upload.count", 1);
            _writer.Add("writer.upload.length", length);
        }
    }
}
