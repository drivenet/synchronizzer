﻿using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class NullObjectWriter : IObjectWriter
    {
        public static NullObjectWriter Instance { get; } = new();

        public Task Delete(string objectName, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task Upload(string objectName, ReadObject readObject, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
