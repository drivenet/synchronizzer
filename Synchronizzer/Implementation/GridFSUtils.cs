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
            try
            {
                var database = client.GetDatabase(url.DatabaseName);
                var bucket = new GridFSBucket<BsonValue>(database);
                return new GridFSContext(bucket, client);
            }
            catch
            {
                client.Dispose();
                throw;
            }
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
