using System;

namespace Synchronizzer.Implementation
{
    internal interface IOriginReader : IObjectSource, IObjectReader, IDisposable
    {
        string Address { get; }
    }
}
