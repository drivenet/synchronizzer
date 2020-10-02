using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class RobustObjectWriter : IObjectWriter
    {
        private readonly IObjectWriter _inner;

        public RobustObjectWriter(IObjectWriter inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public async Task Delete(string objectName, CancellationToken cancellationToken)
        {
            if (objectName is null)
            {
                throw new ArgumentNullException(nameof(objectName));
            }

            try
            {
                await _inner.Delete(objectName, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
#pragma warning disable CA1031 // Do not catch general exception types -- failing to delete something is not considered a real problem
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
            }
        }

        public async Task Upload(string objectName, Stream readOnlyInput, CancellationToken cancellationToken)
        {
            if (objectName is null)
            {
                throw new ArgumentNullException(nameof(objectName));
            }

            try
            {
                await _inner.Upload(objectName, readOnlyInput, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
#pragma warning disable CA1031 // Do not catch general exception types -- it's better to continue synchronizing instead of failing completely
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
            }
        }
    }
}
