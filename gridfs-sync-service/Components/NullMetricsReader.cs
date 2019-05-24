namespace GridFSSyncService.Components
{
    internal sealed class NullMetricsReader : IMetricsReader
    {
        public double? GetValue(string itemName) => null;
    }
}
