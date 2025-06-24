using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

        public async IAsyncEnumerable<IReadOnlyList<ObjectInfo>> GetOrdered(bool nice, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var filter = Builders.Filter.Empty;
            const int Limit = 1000;
            var options = new GridFSFindOptions<BsonValue>
            {
                Sort = FilenameSort,
                BatchSize = (Limit / 2) + 1,
            };

            Task<IReadOnlyList<ObjectInfo>>? nextTask = null;
            while (true)
            {
                nextTask ??= Next();
                var result = await nextTask;
                if (result.Count == 0)
                {
                    break;
                }

                filter = Builders.Filter.Gt(info => info.Filename, result[^1].Name);
                if (!nice)
                {
                    nextTask = Next();
                }

                yield return result;

                if (nice)
                {
                    nextTask = Next();
                }
            }

            async Task<IReadOnlyList<ObjectInfo>> Next()
            {
                return await GridFSUtils.SafeExecute(
                    async () =>
                    {
                        var result = new List<ObjectInfo>(Limit);
                        using var infos = await _context.Bucket.FindAsync(filter, options, cancellationToken);
                        try
                        {
                            await infos.ForEachAsync(
                                info =>
                                {
                                    var objectInfo = new ObjectInfo(info.Filename, info.Length, false, info.UploadDateTime, null);
                                    var lastIndex = result.Count - 1;
                                    if (lastIndex >= 0
                                        && result[lastIndex].Name == objectInfo.Name)
                                    {
                                        result.RemoveAt(lastIndex);
                                    }
                                    else if (lastIndex == Limit - 1)
                                    {
                                        throw new OperationCanceledException();
                                    }

                                    result.Add(objectInfo);
                                },
                                cancellationToken);
                        }
                        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                        {
                        }

                        return result;
                    },
                    10,
                    cancellationToken);
            }
        }
    }
}
