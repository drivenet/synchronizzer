using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Synchronizzer.Implementation
{
    internal static class GridFSUtils
    {
        public static GridFSContext CreateContext(string address)
        {
            var url = MongoUrl.Create(address);
            var client = new MongoClient(url);
            var database = client.GetDatabase(url.DatabaseName);
            var bucket = new GridFSBucket<BsonValue>(
                database,
#pragma warning disable CS0618 // Type or member is obsolete -- disabling is still needed for current version
                new GridFSBucketOptions { DisableMD5 = true });
#pragma warning restore CS0618 // Type or member is obsolete
            return new GridFSContext(bucket);
        }

        public static async Task<T> SafeExecute<T>(Func<Task<T>> action, byte retries, CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    return await action();
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (MongoException) when (retries > 0)
                {
                }
                catch (IOException) when (retries > 0)
                {
                }
                catch (TimeoutException) when (retries > 0)
                {
                }
                catch (SocketException) when (retries > 0)
                {
                }

                await Task.Delay(1511, cancellationToken);
                --retries;
            }
        }
    }
}
