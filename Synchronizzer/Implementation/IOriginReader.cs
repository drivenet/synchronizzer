namespace Synchronizzer.Implementation
{
    internal interface IOriginReader : IObjectSource, IObjectReader
    {
        string Address { get; }
    }
}
