﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        public TracingObjectSource(IObjectSource inner, string source, ILogger<TracingObjectSource> logger)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async IAsyncEnumerable<IReadOnlyList<ObjectInfo>> GetOrdered(bool nice, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            IAsyncEnumerator<IReadOnlyList<ObjectInfo>>? enumerator = null;
            try
            {
                while (true)
                {
                    var timer = Stopwatch.StartNew();
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
                                timer.Elapsed.TotalMilliseconds,
                                cancellationToken.IsCancellationRequested);
                            throw;
                        }
                        catch (Exception exception)
                        {
                            _logger.LogError(exception, "Failed to get objects, elapsed {Elapsed}.", timer.Elapsed.TotalMilliseconds);
                            throw;
                        }

                        if (result.Count == 0)
                        {
                            _logger.LogInformation(Events.Got, "Got no objects, elapsed {Elapsed}.", timer.Elapsed.TotalMilliseconds);
                        }
                        else
                        {
                            _logger.LogInformation(
                                Events.Got,
                                "Got objects, count {Count} (\"{From}\"->\"{To}\"), elapsed {Elapsed}.",
                                result.Count,
                                result[0].Name,
                                result[^1].Name,
                                timer.Elapsed.TotalMilliseconds);
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
