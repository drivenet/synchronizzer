using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Amazon.S3;
using Amazon.S3.Model;

namespace Synchronizzer.Implementation
{
    internal sealed class S3ObjectWriter : IObjectWriter
    {
        private const int PartSize = 128 << 20;

        private readonly S3WriteContext _context;
        private readonly S3WriteContext? _recycleContext;

        public S3ObjectWriter(S3WriteContext context, S3WriteContext? recycleContext)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            if (recycleContext is not null)
            {
                var recyclePrefix = recycleContext.S3.Prefix;
                var prefix = context.S3.Prefix;
                if (recyclePrefix != prefix)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(recycleContext),
                        FormattableString.Invariant($"The recycle prefix \"{recyclePrefix}\" does not match base prefix \"{prefix}\"."));
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

            if (_recycleContext is not null)
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
                    await _recycleContext.S3.Invoke(
                        (s3, token) => s3.CopyObjectAsync(copyRequest, token),
                        $"copy \"{copyRequest.SourceKey}\"@{copyRequest.SourceBucket} to \"{copyRequest.DestinationKey}\"@{copyRequest.DestinationBucket}",
                        cancellationToken);
                }
                catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return;
                }
            }

            await _context.S3.Invoke(
                (s3, token) => s3.DeleteObjectAsync(deleteRequest, token),
                $"delete \"{deleteRequest.Key}\"@{deleteRequest.BucketName}",
                cancellationToken);
        }

        public async Task Upload(string objectName, ReadObject readObject, CancellationToken cancellationToken)
        {
            if (readObject.Length <= PartSize)
            {
                await Put(objectName, readObject, cancellationToken);
                return;
            }

            await MultipartUpload(objectName, readObject, cancellationToken);
        }

        private async Task Put(string objectName, ReadObject readObject, CancellationToken cancellationToken)
        {
            var request = new PutObjectRequest
            {
                BucketName = _context.BucketName,
                Key = objectName,
                InputStream = readObject.Stream,
                AutoCloseStream = false,
                AutoResetStreamPosition = true,
                StorageClass = _context.StorageClass,
                DisablePayloadSigning = true,
            };
            await _context.S3.Invoke(
                (s3, token) => s3.PutObjectAsync(request, token),
                $"put \"{request.Key}\"@{request.BucketName}",
                cancellationToken);
        }

        private async Task MultipartUpload(string objectName, ReadObject readObject, CancellationToken cancellationToken)
        {
            var uploadId = await InitiateUpload(objectName, cancellationToken);
            List<PartETag> etags;
            try
            {
                etags = await UploadParts(objectName, readObject, uploadId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                await AbortUpload(objectName, uploadId, CancellationToken.None);
                throw;
            }
            catch
            {
                await AbortUpload(objectName, uploadId, cancellationToken);
                throw;
            }

            await CompleteUpload(objectName, uploadId, etags, cancellationToken);
        }

        private async Task AbortUpload(string objectName, string uploadId, CancellationToken cancellationToken)
        {
            var completeRequest = new AbortMultipartUploadRequest
            {
                BucketName = _context.BucketName,
                Key = objectName,
                UploadId = uploadId,
            };
            await _context.S3.Invoke(
                (s3, token) => s3.AbortMultipartUploadAsync(completeRequest, token),
                $"abort multipart upload \"{uploadId}\" for \"{completeRequest.Key}\"@{completeRequest.BucketName}",
                cancellationToken);
        }

        private async Task<string> InitiateUpload(string objectName, CancellationToken cancellationToken)
        {
            var initiateRequest = new InitiateMultipartUploadRequest
            {
                BucketName = _context.BucketName,
                Key = objectName,
                StorageClass = _context.StorageClass,
            };

            var initiateResponse = await _context.S3.Invoke(
                (s3, token) => s3.InitiateMultipartUploadAsync(initiateRequest, token),
                $"initiate multipart upload for \"{initiateRequest.Key}\"@{initiateRequest.BucketName}",
                cancellationToken);
            return initiateResponse.UploadId;
        }

        private async Task<List<PartETag>> UploadParts(string objectName, ReadObject readObject, string uploadId, CancellationToken cancellationToken)
        {
            var remaining = readObject.Length;
            var partsCount = checked((int)((remaining + PartSize - 1) / PartSize));
            if (partsCount > 10000)
            {
                throw new InvalidDataException($"The file \"{objectName}\"@{_context.BucketName} is too large, size: {remaining}.");
            }

            var etags = new List<PartETag>();
            var partNumber = 1;
            while (true)
            {
                var request = new UploadPartRequest
                {
                    BucketName = _context.BucketName,
                    Key = objectName,
                    UploadId = uploadId,
                    InputStream = readObject.Stream,
                    DisablePayloadSigning = true,
                    PartNumber = partNumber,
                };

                if (remaining > PartSize)
                {
                    request.PartSize = PartSize;
                    remaining -= PartSize;
                }
                else
                {
                    request.PartSize = remaining;
                    remaining = 0;
                }

                var response = await _context.S3.Invoke(
                    async (s3, token) =>
                    {
                        var position = request.InputStream.Position;
                        try
                        {
                            return await s3.UploadPartAsync(request, token);
                        }
                        catch
                        {
                            request.InputStream.Position = position;
                            throw;
                        }
                    },
                    $"upload part #{request.PartNumber}/{partsCount} of upload \"{request.UploadId}\" for \"{request.Key}\"@{request.BucketName}, size {request.PartSize}",
                    cancellationToken);
                etags.Add(new(response));
                if (remaining == 0)
                {
                    break;
                }

                ++partNumber;
            }

            return etags;
        }

        private async Task CompleteUpload(string objectName, string uploadId, List<PartETag> etags, CancellationToken cancellationToken)
        {
            var completeRequest = new CompleteMultipartUploadRequest
            {
                BucketName = _context.BucketName,
                Key = objectName,
                UploadId = uploadId,
                PartETags = etags,
            };
            await _context.S3.Invoke(
                (s3, token) => s3.CompleteMultipartUploadAsync(completeRequest, token),
                $"complete multipart upload \"{completeRequest.UploadId}\" (etags: {completeRequest.PartETags.Count}) for \"{completeRequest.Key}\"@{completeRequest.BucketName}",
                cancellationToken);
        }
    }
}
