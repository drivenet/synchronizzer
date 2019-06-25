using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.S3.Model;

namespace Synchronizzer.Implementation
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
            var request = new ListObjectsV2Request
            {
                BucketName = _context.BucketName,
                StartAfter = fromName,
            };
            var response = await _context.S3.ListObjectsV2Async(request, cancellationToken);
            var s3Objects = response.S3Objects;
            var result = new List<ObjectInfo>(s3Objects.Count);
            result.AddRange(s3Objects.Select(
                s3Object =>
                {
                    var objectName = s3Object.Key.TrimEnd('/');
                    var isHidden = objectName.StartsWith(S3Constants.LockPrefix, StringComparison.OrdinalIgnoreCase);
                    return new ObjectInfo(objectName, s3Object.Size, isHidden);
                }));
            return result;
        }
    }
}
