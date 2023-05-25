using System;
using System.Collections.Generic;
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

        public IAsyncEnumerable<IReadOnlyCollection<ObjectInfo>> GetOrdered(bool nice, CancellationToken cancellationToken)
            => GetOrdered(nice, null, cancellationToken);

        private async IAsyncEnumerable<IReadOnlyCollection<ObjectInfo>> GetOrdered(bool nice, string? prefix, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            const int MaxKeys = 1000;
            var list = new List<(string Key, long Size, DateTime Timestamp)>(MaxKeys);
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
                list.Clear();
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

                PopulateList(response, list);
                List<ObjectInfo>? result = null;
                foreach (var (key, size, timestamp) in list)
                {
                    if (size < 0)
                    {
                        if (result is { Count: not 0 })
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
                        result ??= new();
                        result.Add(new(key, size, isHidden, timestamp));
                    }
                }

                if (result is { Count: not 0 })
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
                return await _context.S3.Invoke((s3, token) => s3.ListObjectsV2Async(request, token), cancellationToken);
            }
        }

        private static void PopulateList(ListObjectsV2Response response, List<(string Key, long Size, DateTime Timestamp)> list)
        {
            var prefixes = response.CommonPrefixes;
            var prefixIndex = 0;
            var prefix = GetNextPrefix();
            foreach (var s3Object in response.S3Objects)
            {
                while (prefix is not null
                    && string.CompareOrdinal(prefix, s3Object.Key) < 0)
                {
                    list.Add((prefix, -1L, PrefixTimestamp));
                    prefix = GetNextPrefix();
                }

                list.Add((s3Object.Key, s3Object.Size, s3Object.LastModified.ToUniversalTime()));
            }

            while (prefix is not null)
            {
                list.Add((prefix, -1L, PrefixTimestamp));
                prefix = GetNextPrefix();
            }

            string? GetNextPrefix() => prefixIndex < prefixes.Count ? prefixes[prefixIndex++] : null;
        }
    }
}
