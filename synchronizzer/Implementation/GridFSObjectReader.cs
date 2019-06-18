using System.IO;
using System.Threading;
using System.Threading.Tasks;

using MongoDB.Driver.GridFS;

namespace Synchronizzer.Implementation
{
    internal sealed class GridFSObjectReader : IObjectReader
    {
        private readonly GridFSContext _context;

        public GridFSObjectReader(GridFSContext context)
        {
            _context = context;
        }

        public async Task<Stream> Read(string objectName, CancellationToken cancellationToken)
            => await _context.Bucket.OpenDownloadStreamByNameAsync(objectName, new GridFSDownloadByNameOptions { Seekable = true }, cancellationToken);
    }
}
