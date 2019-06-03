using System;

namespace GridFSSyncService.Components
{
    public interface ITimeSource
    {
        DateTime Now { get; }
    }
}
