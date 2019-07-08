using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class RobustObjectReader : IObjectReader
    {
        private readonly IObjectReader _inner;

        public RobustObjectReader(IObjectReader inner)
        {
            _inner = inner;
        }

        public async Task<Stream?> Read(string objectName, CancellationToken cancellationToken)
        {
            try
            {
                return await _inner.Read(objectName, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
#pragma warning disable CA1031 // Do not catch general exception types -- failing to read file just skips it
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                return null;
            }
        }
    }
}
