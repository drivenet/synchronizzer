using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Amazon.S3;
using Amazon.S3.Model;

namespace Synchronizzer.Implementation
{
#pragma warning disable CA1001 // Types that own disposable fields should be disposable -- _lock does not need disposal because wait handle is never allocated
    internal sealed class S3ObjectWriterLocker : IObjectWriterLocker
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        private const string LockExtension = ".lock";
        private static readonly TimeSpan LockInterval = TimeSpan.FromSeconds(17);

        private readonly Stopwatch _timer = new Stopwatch();
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);
        private readonly string _lockName = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        private readonly S3WriteContext _context;

        public S3ObjectWriterLocker(S3WriteContext context)
        {
            _context = context;
        }

        public async Task Lock(CancellationToken cancellationToken)
        {
            try
            {
                await _lock.WaitAsync(cancellationToken);
                if (!_timer.IsRunning
                    || _timer.Elapsed > LockInterval)
                {
                    await LockCore(cancellationToken);
                    _timer.Restart();
                }
            }
            finally
            {
                try
                {
                    _lock.Release();
                }
                catch (SemaphoreFullException)
                {
                }
            }
        }


        public async Task Clear(CancellationToken cancellationToken)
        {
            var deleteObjectRequest = new DeleteObjectRequest
            {
                BucketName = _context.BucketName,
                Key = S3Constants.LockPrefix + _lockName + LockExtension,
            };
            await _context.S3.DeleteObjectAsync(deleteObjectRequest/*, cancellationToken*/ /* No cancellation token to clean up lock properly */);
        }

        private async Task LockCore(CancellationToken cancellationToken)
        {
            var nowTask = GetUtcTime(cancellationToken);
            var tasks = new List<Task>();
            var lockLifetime = TimeSpan.FromMinutes(3);
            var listRequest = new ListObjectsV2Request
            {
                BucketName = _context.BucketName,
                Prefix = S3Constants.LockPrefix,
            };
            while (true)
            {
                var listResponse = await _context.S3.ListObjectsV2Async(listRequest, cancellationToken);
                var threshold = (await nowTask) - lockLifetime;
                var locked = false;
                foreach (var s3Object in listResponse.S3Objects)
                {
                    var key = s3Object.Key;
                    var lockTime = s3Object.LastModified.ToUniversalTime();
                    if (lockTime < threshold)
                    {
                        var deleteObjectRequest = new DeleteObjectRequest
                        {
                            BucketName = _context.BucketName,
                            Key = key,
                        };
                        var deleteTask = _context.S3.DeleteObjectAsync(deleteObjectRequest, cancellationToken);
                        tasks.Add(deleteTask);
                    }
                    else if (key.EndsWith(LockExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        var keyData = key.Substring(S3Constants.LockPrefix.Length, key.Length - LockExtension.Length - S3Constants.LockPrefix.Length);
                        var comparison = string.CompareOrdinal(keyData, _lockName);
                        if (comparison < 0)
                        {
                            throw new OperationCanceledException(FormattableString.Invariant($"The lock \"{_lockName}\" was overriden by \"{keyData}\" ({lockTime:o})."));
                        }

                        if (comparison == 0)
                        {
                            locked = true;
                            break;
                        }
                    }

                    listRequest.StartAfter = s3Object.Key;
                }

                if (locked || !listResponse.IsTruncated)
                {
                    break;
                }
            }

            var request = new PutObjectRequest
            {
                BucketName = _context.BucketName,
                Key = S3Constants.LockPrefix + _lockName + LockExtension,
            };
            var putTask = _context.S3.PutObjectAsync(request, cancellationToken);
            tasks.Add(putTask);
            await Task.WhenAll(tasks);
        }

        private async Task<DateTime> GetUtcTime(CancellationToken cancellationToken)
        {
            var key = S3Constants.LockPrefix + _lockName + ".time";
            var putRequest = new PutObjectRequest
            {
                BucketName = _context.BucketName,
                Key = key,
            };
            var getMetadataRequest = new GetObjectMetadataRequest
            {
                BucketName = _context.BucketName,
                Key = key,
            };
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _context.BucketName,
                Key = key,
            };

            try
            {
                await _context.S3.PutObjectAsync(putRequest, cancellationToken);
                DateTime lastModified;
                var timer = Stopwatch.StartNew();
                while (true)
                {
                    try
                    {
                        lastModified = (await _context.S3.GetObjectMetadataAsync(getMetadataRequest, cancellationToken)).LastModified;
                        break;
                    }
                    catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound && timer.Elapsed < LockInterval)
                    {
                    }
                }

                return lastModified.ToUniversalTime();
            }
            finally
            {
                try
                {
                    await _context.S3.DeleteObjectAsync(deleteRequest, cancellationToken);
                }
                catch (AmazonS3Exception)
                {
                }
            }
        }
    }
}
