using System;

using Amazon.S3;

namespace Synchronizzer.Implementation
{
    internal class S3Context
    {
        public S3Context(IS3Mediator s3, string bucketName)
        {
            S3 = s3 ?? throw new ArgumentNullException(nameof(s3));
            if (!Amazon.S3.Util.AmazonS3Util.ValidateV2Bucket(bucketName))
            {
                throw new ArgumentOutOfRangeException(nameof(bucketName), bucketName, "Invalid bucket name.");
            }

            BucketName = bucketName;
        }

        public IS3Mediator S3 { get; }

        public string BucketName { get; }
    }
}
