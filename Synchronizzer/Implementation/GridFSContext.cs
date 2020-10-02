using System;

using MongoDB.Bson;
using MongoDB.Driver.GridFS;

namespace Synchronizzer.Implementation
{
    internal sealed class GridFSContext
    {
        public GridFSContext(IGridFSBucket<BsonValue> bucket)
        {
            Bucket = bucket ?? throw new ArgumentNullException(nameof(bucket));
        }

        public IGridFSBucket<BsonValue> Bucket { get; }
    }
}
