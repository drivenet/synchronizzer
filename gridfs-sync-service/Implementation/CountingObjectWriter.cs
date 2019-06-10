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

        public CountingObjectWriter(IObjectWriter inner, IMetricsWriter writer)
        {
            _inner = inner;
            _writer = writer;
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
                _writer.Add("writer.delete_errors", 1);
                throw;
            }

            _writer.Add("writer.deletes", 1);
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
                _writer.Add("writer.flush_errors", 1);
                throw;
            }

            _writer.Add("writer.flushes", 1);
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
                _writer.Add("writer.upload_errors", 1);
                throw;
            }

            _writer.Add("writer.uploads", 1);
            _writer.Add("writer.uploads_length", length);
        }
    }
}
