using System;
using System.Threading;
using System.Threading.Tasks;

using MongoDB.Driver.GridFS;

namespace Synchronizzer.Implementation
{
    internal sealed class GridFSFilteringObjectReader : IObjectReader
    {
        private readonly IObjectReader _inner;

        public GridFSFilteringObjectReader(IObjectReader inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public async Task<ReadObject?> Read(string objectName, CancellationToken cancellationToken)
        {
            try
            {
                return await _inner.Read(objectName, cancellationToken);
            }
            catch (GridFSChunkException exception) when (
                exception.Message.StartsWith("GridFS chunk ", StringComparison.Ordinal)
                && exception.Message.EndsWith(" is missing.", StringComparison.Ordinal))
            {
                return null;
            }
        }
    }
}
