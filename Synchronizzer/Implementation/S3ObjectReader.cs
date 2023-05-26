using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Amazon.S3;
using Amazon.S3.Model;

namespace Synchronizzer.Implementation
{
    internal sealed class S3ObjectReader : IObjectReader
    {
        private readonly S3Context _context;

        public S3ObjectReader(S3Context context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<ReadObject?> Read(string objectName, CancellationToken cancellationToken)
        {
            var request = new GetObjectRequest
            {
                BucketName = _context.BucketName,
                Key = objectName,
            };

            try
            {
                var response = await _context.S3.Invoke(
                    (s3, token) => s3.GetObjectAsync(request, token),
                    $"get \"{request.Key}\"@{request.BucketName}",
                    cancellationToken);
                return new ReadObject(response.ResponseStream, response.ContentLength);
            }
            catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }
    }
}
