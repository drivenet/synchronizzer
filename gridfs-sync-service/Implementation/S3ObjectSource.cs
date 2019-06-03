using System;
using System.Collections.Generic;
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

        public async Task<IReadOnlyCollection<ObjectInfo>> GetOrdered(string? fromName, CancellationToken cancellationToken)
        {
            var response = await _context.S3.ListObjectsAsync(_context.BucketName, fromName, cancellationToken);
            var s3Objects = response.S3Objects;
            var count = s3Objects.Count;
            if (count == 0)
            {
                return Array.Empty<ObjectInfo>();
            }

            var result = new List<ObjectInfo>(count);
            foreach (var s3Object in s3Objects)
            {
                result.Add(new ObjectInfo(s3Object.Key, s3Object.Size));
            }

            return result;
        }
    }
}
