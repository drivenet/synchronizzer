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
#pragma warning disable CA2000 // Dispose objects before losing scope -- introducing lifetime management isn't worth it right now
            var client = new MongoClient(url);
#pragma warning restore CA2000 // Dispose objects before losing scope
            var database = client.GetDatabase(url.DatabaseName);
            var bucket = new GridFSBucket<BsonValue>(database);
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
