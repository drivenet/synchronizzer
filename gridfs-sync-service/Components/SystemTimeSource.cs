using System;

namespace GridFSSyncService.Components
{
    internal sealed class SystemTimeSource : ITimeSource
    {
        public static SystemTimeSource Instance { get; } = new SystemTimeSource();

        public DateTime Now => DateTime.UtcNow;
    }
}
