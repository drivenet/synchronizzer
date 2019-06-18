namespace GridFSSyncService.Components
{
    internal interface IMetricsReader
    {
        double? GetValue(string itemName);
    }
}
