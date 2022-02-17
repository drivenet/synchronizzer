using System;
using System.Collections.Generic;
using System.Net;

using Amazon;
using Amazon.Runtime;
using Amazon.S3;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

using Synchronizzer.Implementation;

namespace Synchronizzer.Composition
{
    internal static class S3Utils
    {
        public static S3Context CreateContext(Uri uri)
        {
            var (s3, bucketName) = CreateContext(uri, out _);
            return new S3Context(CreateS3Mediator(s3), bucketName);
        }

        public static S3WriteContext CreateWriteContext(Uri uri)
        {
            var (s3, bucketName) = CreateContext(uri, out var query);
            query.TryGetValue("class", out var storageClassString);
            var storageClass = ParseStorageClass(storageClassString);
            return new S3WriteContext(CreateS3Mediator(s3), bucketName, storageClass);
        }

        private static (IAmazonS3 S3, string BucketName) CreateContext(Uri uri, out Dictionary<string, StringValues> query)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

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

            var credentials = new BasicAWSCredentials(WebUtility.UrlDecode(userInfo[0]), WebUtility.UrlDecode(userInfo[1]));

            var bucketName = uri.AbsolutePath.TrimStart('/');
            query = QueryHelpers.ParseQuery(uri.Query);
            var host = uri.Host;

            var config = new AmazonS3Config
            {
                ForcePathStyle = true,
                DisableLogging = true,
                Timeout = TimeSpan.FromSeconds(47),
                RetryMode = RequestRetryMode.Standard,
                MaxErrorRetry = 10,
            };
            RegionEndpoint? regionEndpoint;
            try
            {
                regionEndpoint = RegionEndpoint.GetBySystemName(host);
                if (regionEndpoint is { DisplayName: "Unknown" })
                {
                    regionEndpoint = null;
                }
            }
            catch (ArgumentException)
            {
                regionEndpoint = null;
            }

            if (regionEndpoint is not null)
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

            var client = new AmazonS3Client(credentials, config);

            return (client, bucketName);
        }

        private static IS3Mediator CreateS3Mediator(IAmazonS3 s3)
            => new CancelationHandlingS3Mediator(
                new TimeoutHandlingS3Mediator(
                    new ExceptionHandlingS3Mediator(
                        new DefaultS3Mediator(s3)),
                    TimeSpan.FromSeconds(127)));

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
