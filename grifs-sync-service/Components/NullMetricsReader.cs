namespace GridFSSyncService.Components
{
    internal sealed class NullMetricsReader : IMetricsReader
    {
        private NullMetricsReader()
        {
        }

        public static NullMetricsReader Instance { get; } = new NullMetricsReader();

        public double? GetValue(string itemName) => null;
    }
}
