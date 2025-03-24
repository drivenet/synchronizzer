using System;

namespace Synchronizzer.Implementation
{
    internal class S3Context : IDisposable
    {
        private readonly IDisposable _disposable;

        public S3Context(IS3Mediator s3, string bucketName, IDisposable disposable)
        {
            S3 = s3 ?? throw new ArgumentNullException(nameof(s3));
            if (!Amazon.S3.Util.AmazonS3Util.ValidateV2Bucket(bucketName))
            {
                throw new ArgumentOutOfRangeException(nameof(bucketName), bucketName, "Invalid bucket name.");
            }

            BucketName = bucketName;
            _disposable = disposable ?? throw new ArgumentNullException(nameof(disposable));
        }

        public IS3Mediator S3 { get; }

        public string BucketName { get; }

        void IDisposable.Dispose() => _disposable.Dispose();
    }
}
