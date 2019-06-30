using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Synchronizzer.Implementation
{
    internal sealed class DebugObjectSource : IObjectSource
    {
        private readonly IObjectSource _inner;
        private readonly ILogger _logger;

        public DebugObjectSource(IObjectSource inner, ILogger<DebugObjectSource> logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<ObjectInfo>> GetOrdered(string? fromName, CancellationToken cancellationToken)
        {
            var result = await _inner.GetOrdered(fromName, cancellationToken);
            _logger.Log(LogLevel.Information, Events.Get, (object?)null, null, (state, exception) => string.Join(' ', result.Select(info => info.Name)));
            return result;
        }

        private static class Events
        {
            public static readonly EventId Get = new EventId(1, nameof(Get));
        }
    }
}
