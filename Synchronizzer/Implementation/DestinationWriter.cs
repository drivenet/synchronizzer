using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class DestinationWriter : IDestinationWriter
    {
        private readonly IObjectSource _source;
        private readonly IObjectWriter _writer;
        private readonly IObjectWriterLocker _locker;

        public DestinationWriter(string address, IObjectSource source, IObjectWriter writer, IObjectWriterLocker locker)
        {
            Address = address ?? throw new ArgumentNullException(nameof(address));
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _locker = locker ?? throw new ArgumentNullException(nameof(locker));
        }

        public string Address { get; }

        public async Task Delete(string objectName, CancellationToken cancellationToken)
        {
            CheckObjectName(objectName);
            await Task.WhenAll(
                _locker.Lock(cancellationToken),
                _writer.Delete(objectName, cancellationToken));
        }

        public async IAsyncEnumerable<IReadOnlyCollection<ObjectInfo>> GetOrdered([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await _locker.Lock(cancellationToken);
            await foreach (var item in _source.GetOrdered(cancellationToken))
            {
                await _locker.Lock(cancellationToken);
                yield return item;
            }
        }

        public Task TryLock(CancellationToken cancellationToken) => _locker.Lock(cancellationToken);

        public async Task Unlock()
        {
            var unlockTimeout = TimeSpan.FromSeconds(13);
            using var cts = new CancellationTokenSource(unlockTimeout);
            await _locker.Clear(cts.Token);
        }

        public async Task Upload(string objectName, ReadObject readObject, CancellationToken cancellationToken)
        {
            if (readObject is null)
            {
                throw new ArgumentNullException(nameof(readObject));
            }

            CheckObjectName(objectName);
            await Task.WhenAll(
                _locker.Lock(cancellationToken),
                _writer.Upload(objectName, readObject, cancellationToken));
        }

        public override string ToString() => Address;

        private static void CheckObjectName(string objectName)
        {
            if (objectName.StartsWith(S3Constants.LockPrefix, StringComparison.OrdinalIgnoreCase)
                || objectName.StartsWith(FilesystemConstants.LockPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentOutOfRangeException(nameof(objectName), objectName, "Cannot use object name that is prefixed with locks.");
            }
        }
    }
}
