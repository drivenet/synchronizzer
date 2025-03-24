using System;

using Amazon.S3;

namespace Synchronizzer.Implementation
{
    internal sealed class S3WriteContext : S3Context
    {
        public S3WriteContext(IS3Mediator s3, string bucketName, S3StorageClass storageClass, IDisposable disposable)
            : base(s3, bucketName, disposable)
        {
            StorageClass = storageClass ?? throw new System.ArgumentNullException(nameof(storageClass));
        }

        public S3StorageClass StorageClass { get; }
    }
}
