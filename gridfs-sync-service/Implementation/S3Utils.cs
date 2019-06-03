using System;

using Amazon.Runtime;
using Amazon.S3;

using Microsoft.Extensions.Configuration;

namespace GridFSSyncService.Implementation
{
    internal static class S3Utils
    {
        public static S3WriteContext CreateContext(IConfiguration configuration)
        {
            var accessKey = configuration.GetValue<string>("accessKey");
            var secretKey = configuration.GetValue<string>("secretKey");
            var bucketName = configuration.GetValue<string>("bucketName");
            var storageClassString = configuration.GetValue<string>("storageClass");
            var regionEndpointString = configuration.GetValue<string>("regionEndpoint");
            var regionEndpoint = regionEndpointString is object
                ? Amazon.RegionEndpoint.GetBySystemName(regionEndpointString)
                : null;
            var serviceUrl = configuration.GetValue<string>("serviceUrl");
            var credentials = new BasicAWSCredentials(accessKey, secretKey);
            var config = new AmazonS3Config
            {
                ServiceURL = serviceUrl,
            };

            if (regionEndpoint is null)
            {
                config.ForcePathStyle = true;
            }
            else
            {
                config.RegionEndpoint = regionEndpoint;
            }

            S3StorageClass storageClass;
            switch (storageClassString)
            {
                case nameof(S3StorageClass.Standard):
                    storageClass = S3StorageClass.Standard;
                    break;

                case nameof(S3StorageClass.StandardInfrequentAccess):
                    storageClass = S3StorageClass.StandardInfrequentAccess;
                    break;

                case nameof(S3StorageClass.ReducedRedundancy):
                    storageClass = S3StorageClass.ReducedRedundancy;
                    break;

                case null:
                case "":
                    throw new ArgumentException("Unspecified storage class.", nameof(storageClass));

                default:
                    throw new ArgumentOutOfRangeException(nameof(storageClass), storageClassString, "Unknown storage class.");
            }

            var client = new AmazonS3Client(credentials, config);
            return new Implementation.S3WriteContext(client, bucketName, storageClass);
        }
    }
}
