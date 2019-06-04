using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal sealed class S3ObjectSource : IObjectSource
    {
        private readonly S3Context _context;

        public S3ObjectSource(S3Context context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ObjectInfo>> GetOrdered(string? fromName, CancellationToken cancellationToken)
        {
            var response = await _context.S3.ListObjectsAsync(_context.BucketName, fromName, cancellationToken);
            var s3Objects = response.S3Objects;
            if (s3Objects.Count == 0)
            {
                return Enumerable.Empty<ObjectInfo>();
            }

            return s3Objects.Select(s3Object => new ObjectInfo(s3Object.Key, s3Object.Size));
        }
    }
}
