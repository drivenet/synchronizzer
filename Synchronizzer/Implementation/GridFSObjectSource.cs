using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly GridFSContext _context;

        public GridFSObjectSource(GridFSContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<ObjectsBatch> GetOrdered(string? continuationToken, CancellationToken cancellationToken)
        {
            FilterDefinition<GridFSFileInfo<BsonValue>> filter;
            if (continuationToken is null)
            {
                filter = Builders.Filter.Empty;
            }
            else if (continuationToken.Length != 0)
            {
                filter = Builders.Filter.Gt(info => info.Filename, continuationToken);
            }
            else
            {
                return ObjectsBatch.Empty;
            }

            const int Limit = 8192;
            var options = new GridFSFindOptions<BsonValue>
            {
                Sort = FilenameSort,
                BatchSize = (Limit / 2) + 1,
            };
            continuationToken = "";
            var result = new List<ObjectInfo>(Limit);
            using (var infos = await _context.Bucket.FindAsync(filter, options, cancellationToken))
            {
                await infos.ForEachAsync(
                    (info, cancel) =>
                    {
                        var objectInfo = new ObjectInfo(info.Filename, info.Length, false);
                        var lastIndex = result.Count - 1;
                        if (lastIndex >= 0
                            && result[lastIndex].Name == objectInfo.Name)
                        {
                            result.RemoveAt(lastIndex);
                        }
                        else if (lastIndex == Limit - 1)
                        {
                            cancel.Cancel();
                        }

                        result.Add(objectInfo);
                        continuationToken = objectInfo.Name;
                    },
                    cancellationToken);
            }

            return new(result, continuationToken);
        }
    }
}
