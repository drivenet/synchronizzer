using System.Collections.Concurrent;

namespace GridFSSyncService.Components
{
    internal sealed class MetricsContainer : IMetricsReader, IMetricsWriter
    {
        private readonly ConcurrentDictionary<string, double> _metrics = new ConcurrentDictionary<string, double>();

        public void Add(string itemName, double value)
            => _metrics.AddOrUpdate(itemName, value, (_, current) => current + value);

        public double? GetValue(string itemName)
            => _metrics.TryGetValue(itemName, out var value) ? value : (double?)null;
    }
}
