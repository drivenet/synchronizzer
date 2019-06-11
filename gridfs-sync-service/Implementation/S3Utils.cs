using System;

using Amazon.Runtime;
using Amazon.S3;

using Microsoft.AspNetCore.WebUtilities;

namespace GridFSSyncService.Implementation
{
    internal static class S3Utils
    {
        public static S3WriteContext CreateContext(Uri uri)
        {
            if (!uri.IsAbsoluteUri
                || !"s3".Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentOutOfRangeException(nameof(uri), "Invalid S3 URI.");
            }

            var query = QueryHelpers.ParseQuery(uri.Query);
            var accessKey = query["accessKey"];
            var secretKey = query["secretKey"];
            var bucketName = uri.AbsolutePath.TrimStart('/');
            var storageClassString = query["class"];
            var host = uri.Host;

            var credentials = new BasicAWSCredentials(accessKey, secretKey);
            var config = new AmazonS3Config
            {
                ForcePathStyle = true,
            };
            var regionEndpoint = Amazon.RegionEndpoint.GetBySystemName(host);
            if (regionEndpoint.DisplayName != "Unknown")
            {
                config.RegionEndpoint = regionEndpoint;
            }
            else
            {
                if (query.TryGetValue("region", out var region))
                {
                    config.AuthenticationRegion = region;
                }

                config.ServiceURL = new UriBuilder(Uri.UriSchemeHttps, host).Uri.AbsoluteUri;
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
            return new S3WriteContext(client, bucketName, storageClass);
        }
    }
}
