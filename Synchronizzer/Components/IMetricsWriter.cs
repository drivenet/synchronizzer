namespace Synchronizzer.Components
{
    internal interface IMetricsWriter
    {
        void Add(string itemName, double value);
    }
}
