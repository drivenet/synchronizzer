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
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<ObjectsBatch> GetOrdered(string? continuationToken, CancellationToken cancellationToken)
        {
            if (continuationToken is { Length: 0 })
            {
                return ObjectsBatch.Empty;
            }

            var request = new ListObjectsV2Request
            {
                BucketName = _context.BucketName,
                ContinuationToken = continuationToken,
            };
            var response = await _context.S3.Invoke((s3, token) => s3.ListObjectsV2Async(request, token), cancellationToken);
            var s3Objects = response.S3Objects;
            var result = new List<ObjectInfo>(s3Objects.Count);
            result.AddRange(s3Objects.Select(
                s3Object =>
                {
                    var objectName = s3Object.Key.TrimEnd('/');
                    var isHidden = objectName.StartsWith(S3Constants.LockPrefix, StringComparison.OrdinalIgnoreCase);
                    return new ObjectInfo(objectName, s3Object.Size, isHidden);
                }));
            return new(result, response.NextContinuationToken ?? "");
        }
    }
}
