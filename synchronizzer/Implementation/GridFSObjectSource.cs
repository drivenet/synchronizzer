using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

using Builders = MongoDB.Driver.Builders<MongoDB.Driver.GridFS.GridFSFileInfo<MongoDB.Bson.BsonValue>>;

namespace Synchronizzer.Implementation
{
    internal sealed class GridFSObjectSource : IObjectSource
    {
        private static readonly SortDefinition<GridFSFileInfo<BsonValue>> FilenameSort
            = Builders.Sort
                .Ascending(info => info.Filename)
                .Ascending(info => info.UploadDateTime);

        private static readonly SortDefinition<GridFSFileInfo<BsonValue>> NewestSort
            = Builders.Sort
                .Descending(info => info.UploadDateTime);

        private readonly GridFSContext _context;

        public GridFSObjectSource(GridFSContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyCollection<ObjectInfo>> GetOrdered(string? fromName, CancellationToken cancellationToken)
        {
            var filter = fromName is object
                ? Builders.Filter.Gt<string>(info => info.Filename, fromName)
                : Builders.Filter.Empty;
            const int BatchSize = 8192;
            var options = new GridFSFindOptions<BsonValue>
            {
                Sort = FilenameSort,
                BatchSize = BatchSize,
                Limit = BatchSize,
            };
            var result = new List<ObjectInfo>(BatchSize);
            using (var infos = await _context.Bucket.FindAsync(filter, options, cancellationToken))
            {
                await infos.ForEachAsync(
                    info =>
                    {
                        var lastIndex = result.Count - 1;
                        if (lastIndex >= 0
                            && result[lastIndex].Name == info.Filename)
                        {
                            result.RemoveAt(lastIndex);
                        }

                        result.Add(new ObjectInfo(info.Filename, info.Length));
                    },
                    cancellationToken);
            }

            var lastIndex = result.Count;
            if (lastIndex == BatchSize)
            {
                --lastIndex;
                var singleFilter = Builders.Filter.Eq(info => info.Filename, result[lastIndex].Name);
                var singleOptions = new GridFSFindOptions<BsonValue>
                {
                    Sort = NewestSort,
                    Limit = 1,
                };
                using (var infos = await _context.Bucket.FindAsync(singleFilter, singleOptions, cancellationToken))
                {
                    result.RemoveAt(lastIndex);
                    GridFSFileInfo<BsonValue>? info = await infos.SingleOrDefaultAsync(cancellationToken);
                    if (info is object)
                    {
                        result.Add(new ObjectInfo(info.Filename, info.Length));
                    }
                }
            }

            return result;
        }
    }
}
