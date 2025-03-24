using System;

using MongoDB.Bson;
using MongoDB.Driver.GridFS;

namespace Synchronizzer.Implementation
{
    internal sealed class GridFSContext : IDisposable
    {
        private readonly IDisposable _disposable;

        public GridFSContext(IGridFSBucket<BsonValue> bucket, IDisposable disposable)
        {
            Bucket = bucket ?? throw new ArgumentNullException(nameof(bucket));
            _disposable = disposable ?? throw new ArgumentNullException(nameof(disposable));
        }

        public IGridFSBucket<BsonValue> Bucket { get; }

        void IDisposable.Dispose() => _disposable.Dispose();
    }
}
