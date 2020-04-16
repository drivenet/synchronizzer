using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Synchronizzer.Implementation
{
    internal static class GridFSUtils
    {
        public static GridFSContext CreateContext(string address)
        {
            var url = MongoUrl.Create(address);
            var client = new MongoClient(url);
            var database = client.GetDatabase(url.DatabaseName);
            var bucket = new GridFSBucket<BsonValue>(database, new GridFSBucketOptions { DisableMD5 = true });
            return new GridFSContext(bucket);
        }
    }
}
