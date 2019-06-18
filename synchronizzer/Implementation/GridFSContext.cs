using MongoDB.Bson;
using MongoDB.Driver.GridFS;

namespace GridFSSyncService.Implementation
{
    internal sealed class GridFSContext
    {
        public GridFSContext(IGridFSBucket<BsonValue> bucket)
        {
            Bucket = bucket;
        }

        public IGridFSBucket<BsonValue> Bucket { get; }
    }
}
