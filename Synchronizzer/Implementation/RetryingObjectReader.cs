﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class RetryingObjectReader : IObjectReader
    {
        private readonly IObjectReader _inner;
        private readonly byte _retries;

        public RetryingObjectReader(IObjectReader inner, byte retries)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _retries = retries;
        }

        public async Task<ReadObject?> Read(string objectName, CancellationToken cancellationToken)
        {
            var retries = _retries;
            while (true)
            {
                try
                {
                    return await _inner.Read(objectName, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
#pragma warning disable CA1031 // Do not catch general exception types -- dumb retry mechanism
                catch when (retries-- > 0)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                }

                await Task.Delay(1511, cancellationToken);
            }
        }
    }
}
