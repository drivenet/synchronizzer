using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Amazon.S3;
using Amazon.S3.Model;

namespace Synchronizzer.Implementation
{
#pragma warning disable CA1001 // Types that own disposable fields should be disposable -- _lock does not need disposal because wait handle is never allocated
    internal sealed class S3ObjectWriter : IObjectWriter
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        private const string LockExtension = ".lock";
        private static readonly TimeSpan LockInterval = TimeSpan.FromSeconds(17);

        private readonly S3WriteContext _context;
        private readonly S3WriteContext? _recycleContext;
        private readonly string _lockName = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        private readonly Stopwatch _timer = new Stopwatch();
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);

        public S3ObjectWriter(S3WriteContext context, S3WriteContext? recycleContext)
        {
            _context = context;
            if (recycleContext is object)
            {
                var url = new Uri(context.S3.Config.DetermineServiceURL(), UriKind.Absolute);
                var recycleUrl = new Uri(recycleContext.S3.Config.DetermineServiceURL(), UriKind.Absolute);
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
            var lockTask = Lock(cancellationToken);
            CheckObjectName(objectName);
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
                    await _recycleContext.S3.CopyObjectAsync(copyRequest, cancellationToken);
                }
                catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await lockTask;
                    return;
                }
            }

            await Task.WhenAll(
                _context.S3.DeleteObjectAsync(deleteRequest, cancellationToken),
                lockTask);
        }

        public async Task Flush(CancellationToken cancellationToken)
        {
            var deleteObjectRequest = new DeleteObjectRequest
            {
                BucketName = _context.BucketName,
                Key = S3Constants.LockPrefix + _lockName + LockExtension,
            };
            await _context.S3.DeleteObjectAsync(deleteObjectRequest/*, cancellationToken*/ /* No cancellation token to clean up lock properly */);
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

        public async Task Upload(string objectName, Stream readOnlyInput, CancellationToken cancellationToken)
        {
            var lockTask = Lock(cancellationToken);
            CheckObjectName(objectName);
            var request = new PutObjectRequest
            {
                BucketName = _context.BucketName,
                Key = objectName,
                InputStream = readOnlyInput,
                StorageClass = _context.StorageClass,
            };
            await Task.WhenAll(
                _context.S3.PutObjectAsync(request, cancellationToken),
                lockTask);
        }

        private static void CheckObjectName(string objectName)
        {
            if (objectName.StartsWith(S3Constants.LockPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentOutOfRangeException(nameof(objectName), objectName, "Cannot use object name that is prefixed with locks.");
            }
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

        private async Task LockCore(CancellationToken cancellationToken)
        {
            var nowTask = GetUtcTime(cancellationToken);
            var tasks = new List<Task>();
            var lockLifetime = TimeSpan.FromMinutes(5);
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
    }
}
