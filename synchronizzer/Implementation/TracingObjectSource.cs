using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Synchronizzer.Implementation
{
    internal sealed class TracingObjectSource : IObjectSource
    {
        private readonly IObjectSource _inner;
        private readonly string _source;
        private readonly ILogger _logger;

        public TracingObjectSource(IObjectSource inner, string source, ILogger<TracingObjectSource> logger)
        {
            _inner = inner;
            _source = source;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<ObjectInfo>> GetOrdered(string? fromName, CancellationToken cancellationToken)
        {
            using (_logger.BeginScope("{Source}", _source))
            {
                var timer = Stopwatch.StartNew();
                IReadOnlyCollection<ObjectInfo> result;
                _logger.LogDebug(Events.Get, "Get \"{From}\".", fromName);
                try
                {
                    result = await _inner.GetOrdered(fromName, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation(Events.GetCanceled, "Get \"{From}\" was canceled, elapsed {Elapsed}.", fromName, timer.Elapsed.TotalMilliseconds);
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Failed to get \"{From}\", elapsed {Elapsed}.", fromName, timer.Elapsed.TotalMilliseconds);
                    throw;
                }

                _logger.LogInformation(Events.Got, "Got \"{From}\", count {Count}, elapsed {Elapsed}.", fromName, result.Count, timer.Elapsed.TotalMilliseconds);
                return result;
            }
        }

        private static class Events
        {
            public static readonly EventId Get = new EventId(1, nameof(Get));
            public static readonly EventId Got = new EventId(2, nameof(Got));
            public static readonly EventId GetCanceled = new EventId(3, nameof(GetCanceled));
        }
    }
}
