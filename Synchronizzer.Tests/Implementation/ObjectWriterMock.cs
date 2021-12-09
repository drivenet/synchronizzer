using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Synchronizzer.Implementation;

namespace Synchronizzer.Tests.Implementation
{
    internal sealed class ObjectWriterMock : IObjectWriter
    {
        private readonly Dictionary<string, Stream> _uploaded = new Dictionary<string, Stream>();
        private readonly HashSet<string> _deleted = new HashSet<string>();

        public Task Delete(string objectName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_uploaded.ContainsKey(objectName))
            {
                throw new ArgumentOutOfRangeException(nameof(objectName), objectName, "Object was uploaded.");
            }

            if (!_deleted.Add(objectName))
            {
                throw new ArgumentOutOfRangeException(nameof(objectName), objectName, "Object was already deleted.");
            }

            return Task.CompletedTask;
        }

        public Task Upload(string objectName, ReadObject readObject, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_deleted.Contains(objectName))
            {
                throw new ArgumentOutOfRangeException(nameof(objectName), objectName, "Object was deleted.");
            }

            if (!_uploaded.TryAdd(objectName, readObject.Stream))
            {
                throw new ArgumentOutOfRangeException(nameof(objectName), objectName, "Object was already uploaded.");
            }

            return Task.CompletedTask;
        }

        public IReadOnlyCollection<(string Name, Stream Input)> GetUploads()
        {
            return _uploaded.Select(pair => (pair.Key, pair.Value)).ToList();
        }

        public IReadOnlyCollection<string> GetDeletes()
        {
            return _deleted.ToList();
        }
    }
}
