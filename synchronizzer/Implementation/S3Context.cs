using System;

using Amazon.S3;

namespace Synchronizzer.Implementation
{
    internal class S3Context
    {
        public S3Context(IAmazonS3 s3, string bucketName)
        {
            S3 = s3;
            if (!Amazon.S3.Util.AmazonS3Util.ValidateV2Bucket(bucketName))
            {
                throw new ArgumentOutOfRangeException(nameof(bucketName), bucketName, "Invalid bucket name.");
            }

            BucketName = bucketName;
        }

        public IAmazonS3 S3 { get; }

        public string BucketName { get; }
    }
}
