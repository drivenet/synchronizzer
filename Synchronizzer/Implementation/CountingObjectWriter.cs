﻿using System;
using System.Threading;
using System.Threading.Tasks;

using Synchronizzer.Components;

namespace Synchronizzer.Implementation
{
    internal sealed class CountingObjectWriter : IObjectWriter
    {
        private readonly IObjectWriter _inner;
        private readonly IMetricsWriter _writer;
        private readonly string _prefix;

        public CountingObjectWriter(IObjectWriter inner, IMetricsWriter writer, string key)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
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

        public async Task Upload(string objectName, ReadObject readObject, CancellationToken cancellationToken)
        {
            if (readObject is null)
            {
                throw new ArgumentNullException(nameof(readObject));
            }

            try
            {
                await _inner.Upload(objectName, readObject, cancellationToken);
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
            _writer.Add(_prefix + "uploads_length", readObject.Length);
        }
    }
}
