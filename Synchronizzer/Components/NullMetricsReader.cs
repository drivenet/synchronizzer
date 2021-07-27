﻿namespace Synchronizzer.Components
{
    internal sealed class NullMetricsReader : IMetricsReader
    {
        public static NullMetricsReader Instance { get; } = new();

        public double? GetValue(string itemName) => null;
    }
}
