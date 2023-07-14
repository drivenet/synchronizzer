using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Amazon.S3.Model;

namespace Synchronizzer.Implementation
{
    internal sealed class S3ObjectSource : IObjectSource
    {
        private static readonly DateTime PrefixTimestamp = new DateTime(0, DateTimeKind.Utc);

        private readonly S3Context _context;

        public S3ObjectSource(S3Context context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IAsyncEnumerable<IReadOnlyList<ObjectInfo>> GetOrdered(bool nice, CancellationToken cancellationToken)
            => GetOrdered(nice, null, cancellationToken);

        private async IAsyncEnumerable<IReadOnlyList<ObjectInfo>> GetOrdered(bool nice, string? prefix, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            const int MaxKeys = 1000;
            var request = new ListObjectsV2Request
            {
                Prefix = prefix,
                BucketName = _context.BucketName,
                Delimiter = "/",
                MaxKeys = MaxKeys,
            };
            Task<ListObjectsV2Response>? nextTask = null;
            while (true)
            {
                nextTask ??= Next();
                var response = await nextTask;
                if (response.NextContinuationToken is { } continuationToken)
                {
                    request.ContinuationToken = continuationToken;
                    if (!nice)
                    {
                        nextTask = Next();
                    }
                }
                else
                {
                    nextTask = null;
                }

                var origin = response.ResponseMetadata.RequestId;
                List<ObjectInfo>? result = null;
                foreach (var (key, size, timestamp) in PopulateList(response))
                {
                    if (size < 0)
                    {
                        if (result is not null)
                        {
                            yield return result;
                            result = null;
                        }

                        await foreach (var prefixedResult in GetOrdered(nice, key, cancellationToken))
                        {
                            if (prefixedResult.Count != 0)
                            {
                                yield return prefixedResult;
                            }
                        }
                    }
                    else
                    {
                        var isHidden = key.StartsWith(S3Constants.LockPrefix, StringComparison.OrdinalIgnoreCase);
                        var info = new ObjectInfo(key, size, isHidden, timestamp, origin);
                        if (result is not null)
                        {
                            var lastInfo = result[^1];
                            if (info.CompareTo(lastInfo) <= 0)
                            {
                                throw new InvalidDataException(FormattableString.Invariant(
                                    $"S3 object info {info} is not sorted wrt {lastInfo}, bucket name \"{_context.BucketName}\"."));
                            }
                        }
                        else
                        {
                            result = new();
                        }

                        result.Add(info);
                    }
                }

                if (result is not null)
                {
                    yield return result;
                }

                if (nextTask is null)
                {
                    break;
                }

                if (nice)
                {
                    nextTask = Next();
                }
            }

            async Task<ListObjectsV2Response> Next()
            {
                return await _context.S3.Invoke(
                    (s3, token) => s3.ListObjectsV2Async(request, token),
                    $"list from \"{request.ContinuationToken}\", prefix \"{request.Prefix}\" @{request.BucketName}",
                    cancellationToken);
            }
        }

        private static IEnumerable<(string Key, long Size, DateTime Timestamp)> PopulateList(ListObjectsV2Response response)
        {
            var prefixes = response.CommonPrefixes;
            var prefixIndex = 0;
            var prefix = GetNextPrefix();
            foreach (var s3Object in response.S3Objects)
            {
                if (s3Object.Key == response.Prefix)
                {
                    continue;
                }

                while (prefix is not null
                    && string.CompareOrdinal(prefix, s3Object.Key) < 0)
                {
                    yield return (prefix, -1L, PrefixTimestamp);
                    prefix = GetNextPrefix();
                }

                yield return (s3Object.Key, s3Object.Size, s3Object.LastModified.ToUniversalTime());
            }

            while (prefix is not null)
            {
                yield return (prefix, -1L, PrefixTimestamp);
                prefix = GetNextPrefix();
            }

            string? GetNextPrefix() => prefixIndex < prefixes.Count ? prefixes[prefixIndex++] : null;
        }
    }
}
