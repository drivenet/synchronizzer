using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Amazon.S3;
using Amazon.S3.Model;

namespace Synchronizzer.Implementation
{
    internal sealed class S3ObjectWriterLocker : IObjectWriterLocker
    {
        private const string LockExtension = ".lock";
        private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(151);

        private readonly S3Context _context;
        private readonly string _lockName;
        private bool _isLocked;

        public S3ObjectWriterLocker(S3Context context, string lockName)
        {
            if (string.IsNullOrEmpty(lockName))
            {
                throw new ArgumentException("Invalid lock name.", nameof(lockName));
            }

            _context = context ?? throw new ArgumentNullException(nameof(context));
            _lockName = lockName;
        }

        public async Task Clear(CancellationToken cancellationToken)
        {
            _isLocked = false;
            var deleteObjectRequest = new DeleteObjectRequest
            {
                BucketName = _context.BucketName,
                Key = S3Constants.LockPrefix + _lockName + LockExtension,
            };
            await _context.S3.Invoke(
                (s3, token) => s3.DeleteObjectAsync(deleteObjectRequest, token),
                cancellationToken);
        }

        public async Task Lock(CancellationToken cancellationToken)
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
                var listResponse = await _context.S3.Invoke((s3, token) => s3.ListObjectsV2Async(listRequest, token), cancellationToken);
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
                        var deleteTask = _context.S3.Invoke((s3, token) => s3.DeleteObjectAsync(deleteObjectRequest, token), cancellationToken);
                        tasks.Add(deleteTask);
                    }
                    else if (key.EndsWith(LockExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        var keyData = key.Substring(S3Constants.LockPrefix.Length, key.Length - LockExtension.Length - S3Constants.LockPrefix.Length);
                        if (keyData == _lockName)
                        {
                            locked = true;
                            break;
                        }

                        var message = _isLocked
                            ? FormattableString.Invariant($"The lock \"{_lockName}\" was overriden by \"{keyData}\" (time: {lockTime:o}, threshold: {threshold:o}).")
                            : FormattableString.Invariant($"The lock \"{_lockName}\" is prevented by \"{keyData}\" (time: {lockTime:o}, threshold: {threshold:o}).");

                        _isLocked = false;
                        throw new OperationCanceledException(message);
                    }

                    listRequest.StartAfter = key;
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
            var putTask = _context.S3.Invoke((s3, token) => s3.PutObjectAsync(request, token), cancellationToken);
            tasks.Add(putTask);
            await Task.WhenAll(tasks);
            _isLocked = true;
        }

        public override string ToString() => FormattableString.Invariant($"S3Locker(\"{_lockName}\")");

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
                await _context.S3.Invoke((s3, token) => s3.PutObjectAsync(putRequest, token), cancellationToken);
                DateTime lastModified;
                var timer = Stopwatch.StartNew();
                while (true)
                {
                    try
                    {
                        lastModified = (await _context.S3.Invoke((s3, token) => s3.GetObjectMetadataAsync(getMetadataRequest, token), cancellationToken)).LastModified;
                        break;
                    }
                    catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound && timer.Elapsed < LockTimeout)
                    {
                    }
                }

                return lastModified.ToUniversalTime();
            }
            finally
            {
                try
                {
                    await _context.S3.Cleanup(s3 => s3.DeleteObjectAsync(deleteRequest));
                }
                catch (AmazonS3Exception)
                {
                }
            }
        }
    }
}
