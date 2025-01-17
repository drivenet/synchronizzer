using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.Extensions.Logging;

namespace Synchronizzer.Implementation
{
    internal sealed class TracingObjectSource : IObjectSource
    {
        private readonly IObjectSource _inner;
        private readonly string _source;
        private readonly ILogger _logger;
        private readonly TimeProvider _timeProvider;

        public TracingObjectSource(IObjectSource inner, string source, ILogger<TracingObjectSource> logger, TimeProvider timeProvider)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public async IAsyncEnumerable<IReadOnlyList<ObjectInfo>> GetOrdered(bool nice, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            IAsyncEnumerator<IReadOnlyList<ObjectInfo>>? enumerator = null;
            try
            {
                while (true)
                {
                    var startedAt = _timeProvider.GetTimestamp();
                    IReadOnlyList<ObjectInfo> result;
                    using (_logger.BeginScope("{Source}", _source))
                    {
                        _logger.LogDebug(Events.Get, "Getting objects.");
                        try
                        {
                            enumerator ??= _inner.GetOrdered(nice, cancellationToken).GetAsyncEnumerator(cancellationToken);
                            if (!await enumerator.MoveNextAsync())
                            {
                                break;
                            }

                            result = enumerator.Current;
                        }
                        catch (OperationCanceledException exception)
                        {
                            _logger.LogWarning(
                                Events.GetCanceled,
                                exception,
                                "Getting objects was canceled, elapsed {Elapsed} (direct: {IsDirect}).",
                                _timeProvider.GetElapsedTime(startedAt).TotalMilliseconds,
                                cancellationToken.IsCancellationRequested);
                            throw;
                        }
                        catch (Exception exception)
                        {
                            _logger.LogError(exception, "Failed to get objects, elapsed {Elapsed}.", _timeProvider.GetElapsedTime(startedAt).TotalMilliseconds);
                            throw;
                        }

                        if (result.Count == 0)
                        {
                            _logger.LogInformation(Events.Got, "Got no objects, elapsed {Elapsed}.", _timeProvider.GetElapsedTime(startedAt).TotalMilliseconds);
                        }
                        else
                        {
                            _logger.LogInformation(
                                Events.Got,
                                "Got objects, count {Count} (\"{From}\"->\"{To}\"), elapsed {Elapsed}.",
                                result.Count,
                                result[0].Name,
                                result[^1].Name,
                                _timeProvider.GetElapsedTime(startedAt).TotalMilliseconds);
                        }
                    }

                    yield return result;
                }
            }
            finally
            {
                if (enumerator is not null)
                {
                    await enumerator.DisposeAsync();
                }
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
