using System;

using Amazon.Runtime;
using Amazon.S3;

using Microsoft.AspNetCore.WebUtilities;

namespace Synchronizzer.Implementation
{
    internal static class S3Utils
    {
        public static S3WriteContext CreateContext(Uri uri)
        {
            if (!uri.IsAbsoluteUri
                || !"s3".Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(uri),
                    uri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.SafeUnescaped),
                    "Invalid S3 URI.");
            }

            var userInfo = uri.UserInfo.Split(':', 2);
            if (userInfo.Length != 2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(uri),
                    uri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.SafeUnescaped),
                    "The S3 URI is missing credentials.");
            }

            var credentials = new BasicAWSCredentials(userInfo[0], userInfo[1]);

            var bucketName = uri.AbsolutePath.TrimStart('/');
            var query = QueryHelpers.ParseQuery(uri.Query);
            var host = uri.Host;

            var config = new AmazonS3Config
            {
                ForcePathStyle = true,
                DisableLogging = true,
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

            query.TryGetValue("class", out var storageClassString);
            var storageClass = ParseStorageClass(storageClassString);
            var client = new AmazonS3Client(credentials, config);
            return new S3WriteContext(client, bucketName, storageClass);
        }

        private static S3StorageClass ParseStorageClass(string? storageClass)
        {
            switch (storageClass)
            {
                case nameof(S3StorageClass.Standard):
                    return S3StorageClass.Standard;

                case nameof(S3StorageClass.StandardInfrequentAccess):
                    return S3StorageClass.StandardInfrequentAccess;

                case nameof(S3StorageClass.ReducedRedundancy):
                    return S3StorageClass.ReducedRedundancy;

                case null:
                case "":
                    throw new ArgumentException("Unspecified storage class.", nameof(storageClass));

                default:
                    throw new ArgumentOutOfRangeException(nameof(storageClass), storageClass, "Unknown storage class.");
            }
        }
    }
}
