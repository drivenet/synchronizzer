using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class RemoteWriter : IRemoteWriter
    {
        private readonly IObjectSource _source;
        private readonly IObjectWriter _writer;
        private readonly IObjectWriterLocker _locker;

        public RemoteWriter(IObjectSource source, IObjectWriter writer, IObjectWriterLocker locker)
        {
            _source = source;
            _writer = writer;
            _locker = locker;
        }

        public async Task Delete(string objectName, CancellationToken cancellationToken)
        {
            CheckObjectName(objectName);
            await Task.WhenAll(
                _locker.Lock(cancellationToken),
                _writer.Delete(objectName, cancellationToken));
        }

        public async Task Flush(CancellationToken cancellationToken)
        {
            try
            {
                await _writer.Flush(cancellationToken);
            }
            finally
            {
                await _locker.Clear(cancellationToken);
            }
        }

        public async Task<IReadOnlyCollection<ObjectInfo>> GetOrdered(string? fromName, CancellationToken cancellationToken)
        {
            var lockTask = _locker.Lock(cancellationToken);
            var getTask = _source.GetOrdered(fromName, cancellationToken);
            await Task.WhenAll(lockTask, getTask);
            return getTask.Result;
        }

        public Task Lock(CancellationToken cancellationToken) => _locker.Lock(cancellationToken);

        public async Task Upload(string objectName, Stream readOnlyInput, CancellationToken cancellationToken)
        {
            CheckObjectName(objectName);
            await Task.WhenAll(
                _locker.Lock(cancellationToken),
                _writer.Upload(objectName, readOnlyInput, cancellationToken));
        }

        private static void CheckObjectName(string objectName)
        {
            if (objectName.StartsWith(S3Constants.LockPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentOutOfRangeException(nameof(objectName), objectName, "Cannot use object name that is prefixed with locks.");
            }
        }
    }
}
