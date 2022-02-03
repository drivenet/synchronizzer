using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

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

        public async IAsyncEnumerable<IReadOnlyCollection<ObjectInfo>> GetOrdered([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            string? continuationToken = null;
            do
            {
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
                yield return result;
                continuationToken = response.NextContinuationToken;
            }
            while (continuationToken is not null);
        }
    }
}
