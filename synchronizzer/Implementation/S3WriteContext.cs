using Amazon.S3;

namespace Synchronizzer.Implementation
{
    internal sealed class S3WriteContext : S3Context
    {
        public S3WriteContext(IAmazonS3 s3, string bucketName, S3StorageClass storageClass)
            : base(s3, bucketName)
        {
            StorageClass = storageClass;
        }

        public S3StorageClass StorageClass { get; }
    }
}
