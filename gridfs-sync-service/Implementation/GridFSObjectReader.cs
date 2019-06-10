using System.IO;
using System.Threading;
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Driver.GridFS;

namespace GridFSSyncService.Implementation
{
    internal sealed class GridFSObjectReader : IObjectReader
    {
        private readonly IGridFSBucket<BsonValue> _gridFS;

        public GridFSObjectReader(IGridFSBucket<BsonValue> gridFS)
        {
            _gridFS = gridFS;
        }

        public async Task<Stream> Read(string objectName, CancellationToken cancellationToken)
            => await _gridFS.OpenDownloadStreamByNameAsync(objectName, cancellationToken: cancellationToken);
    }
}
