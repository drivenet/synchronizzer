using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using GridFSSyncService.Implementation;

namespace GridFSSyncService.Tests.Implementation
{
    internal sealed class ObjectWriterMock : IObjectWriter
    {
        private readonly Dictionary<string, Stream> _uploaded = new Dictionary<string, Stream>();
        private readonly HashSet<string> _deleted = new HashSet<string>();
        private bool _isDirty;

        public Task Delete(string objectName)
        {
            if (_uploaded.ContainsKey(objectName))
            {
                throw new ArgumentOutOfRangeException(nameof(objectName), objectName, "Object was uploaded.");
            }

            if (!_deleted.Add(objectName))
            {
                throw new ArgumentOutOfRangeException(nameof(objectName), objectName, "Object was already deleted.");
            }

            _isDirty = true;
            return Task.CompletedTask;
        }

        public Task Flush()
        {
            _isDirty = false;
            return Task.CompletedTask;
        }

        public Task Upload(string objectName, Stream readOnlyInput)
        {
            if (_deleted.Contains(objectName))
            {
                throw new ArgumentOutOfRangeException(nameof(objectName), objectName, "Object was deleted.");
            }

            if (!_uploaded.TryAdd(objectName, readOnlyInput))
            {
                throw new ArgumentOutOfRangeException(nameof(objectName), objectName, "Object was already uploaded.");
            }

            _isDirty = true;
            return Task.CompletedTask;
        }

        public IReadOnlyCollection<(string Name, Stream Input)> GetUploads()
        {
            EnsureClean();
            return _uploaded.Select(pair => (pair.Key, pair.Value)).ToList();
        }

        public IReadOnlyCollection<string> GetDeletes()
        {
            EnsureClean();
            return _deleted.ToList();
        }

        private void EnsureClean()
        {
            if (_isDirty)
            {
                throw new InvalidOperationException("Dirty object writer, flush first.");
            }
        }
    }
}
