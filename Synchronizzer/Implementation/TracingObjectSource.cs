using System;
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
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ObjectsBatch> GetOrdered(string? continuationToken, CancellationToken cancellationToken)
        {
            using (_logger.BeginScope("{Source}", _source))
            {
                var timer = Stopwatch.StartNew();
                ObjectsBatch result;
                _logger.LogDebug(Events.Get, "Get \"{From}\".", continuationToken);
                try
                {
                    result = await _inner.GetOrdered(continuationToken, cancellationToken);
                }
                catch (OperationCanceledException exception)
                {
                    _logger.LogInformation(
                        Events.GetCanceled,
                        exception,
                        "Get \"{From}\" was canceled, elapsed {Elapsed} (direct: {IsDirect}).",
                        continuationToken,
                        timer.Elapsed.TotalMilliseconds,
                        cancellationToken.IsCancellationRequested);
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Failed to get \"{From}\", elapsed {Elapsed}.", continuationToken, timer.Elapsed.TotalMilliseconds);
                    throw;
                }

                _logger.LogInformation(Events.Got, "Got \"{From}\", count {Count}, elapsed {Elapsed}.", continuationToken, result.Count, timer.Elapsed.TotalMilliseconds);
                return result;
            }
        }

        private static class Events
        {
            public static readonly EventId Get = new(1, nameof(Get));
            public static readonly EventId Got = new(2, nameof(Got));
            public static readonly EventId GetCanceled = new(3, nameof(GetCanceled));
        }
    }
}
