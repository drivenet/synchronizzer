namespace Synchronizzer.Components
{
    internal interface IMetricsReader
    {
        double? GetValue(string itemName);
    }
}
