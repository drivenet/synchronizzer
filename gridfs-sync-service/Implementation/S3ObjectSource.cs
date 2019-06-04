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
            var count = s3Objects.Count;
            var skip = 0;
            if (count != 0 && s3Objects[0].Key == fromName)
            {
                skip = 1;
            }

            if (count <= skip)
            {
                return Enumerable.Empty<ObjectInfo>();
            }

            return s3Objects
                .Skip(skip)
                .Select(s3Object => new ObjectInfo(s3Object.Key, s3Object.Size));
        }
    }
}
