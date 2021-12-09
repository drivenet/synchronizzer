﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class RobustObjectReader : IObjectReader
    {
        private readonly IObjectReader _inner;

        public RobustObjectReader(IObjectReader inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public async Task<ReadObject?> Read(string objectName, CancellationToken cancellationToken)
        {
            if (objectName is null)
            {
                throw new ArgumentNullException(nameof(objectName));
            }

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
