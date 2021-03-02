using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Amazon.S3;
using Amazon.S3.Model;

namespace Synchronizzer.Implementation
{
    internal sealed class S3ObjectWriter : IObjectWriter
    {
        private readonly S3WriteContext _context;
        private readonly S3WriteContext? _recycleContext;

        public S3ObjectWriter(S3WriteContext context, S3WriteContext? recycleContext)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            if (recycleContext is object)
            {
                var url = context.S3.ServiceUrl;
                var recycleUrl = recycleContext.S3.ServiceUrl;
                if (Uri.Compare(url, recycleUrl, UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.SafeUnescaped, StringComparison.Ordinal) != 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(recycleContext),
                        FormattableString.Invariant($"The recycle URL \"{recycleUrl}\" does not match base URL \"{url}\"."));
                }

                _recycleContext = recycleContext;
            }
        }

        public async Task Delete(string objectName, CancellationToken cancellationToken)
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _context.BucketName,
                Key = objectName,
            };

            if (_recycleContext is object)
            {
                var copyRequest = new CopyObjectRequest
                {
                    SourceBucket = _context.BucketName,
                    SourceKey = objectName,
                    DestinationBucket = _recycleContext.BucketName,
                    DestinationKey = objectName,
                    StorageClass = _recycleContext.StorageClass,
                };

                try
                {
                    await _recycleContext.S3.Invoke((s3, token) => s3.CopyObjectAsync(copyRequest, token), cancellationToken);
                }
                catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return;
                }
            }

            await _context.S3.Invoke((s3, token) => s3.DeleteObjectAsync(deleteRequest, token), cancellationToken);
        }

        public async Task Upload(string objectName, Stream readOnlyInput, CancellationToken cancellationToken)
        {
            var request = new PutObjectRequest
            {
                BucketName = _context.BucketName,
                Key = objectName,
                InputStream = readOnlyInput,
                AutoCloseStream = false,
                StorageClass = _context.StorageClass,
                DisablePayloadSigning = true,
            };
            await _context.S3.Invoke((s3, token) => s3.PutObjectAsync(request, token), cancellationToken);
        }
    }
}
