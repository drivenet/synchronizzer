using System;
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
        private readonly string _prefix;

        public CountingObjectWriter(IObjectWriter inner, IMetricsWriter writer, string key)
        {
            _inner = inner;
            _writer = writer;
            _prefix = "writer.";
            if (key.Length != 0)
            {
                _prefix = _prefix + key + ".";
            }
        }

        public async Task Delete(string objectName, CancellationToken cancellationToken)
        {
            try
            {
                await _inner.Delete(objectName, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                _writer.Add(_prefix + "delete_errors", 1);
                throw;
            }

            _writer.Add(_prefix + "deletes", 1);
        }

        public async Task Flush(CancellationToken cancellationToken)
        {
            try
            {
                await _inner.Flush(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                _writer.Add(_prefix + "flush_errors", 1);
                throw;
            }

            _writer.Add(_prefix + "flushes", 1);
        }

        public async Task Upload(string objectName, Stream readOnlyInput, CancellationToken cancellationToken)
        {
            long length;
            try
            {
                length = readOnlyInput.Length;
                await _inner.Upload(objectName, readOnlyInput, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                _writer.Add(_prefix + "upload_errors", 1);
                throw;
            }

            _writer.Add(_prefix + "uploads", 1);
            _writer.Add(_prefix + "uploads_length", length);
        }
    }
}
