using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Amazon.S3;

using Microsoft.Extensions.Logging;

using static System.FormattableString;

namespace Synchronizzer.Implementation
{
    internal sealed class TracingS3Mediator : IS3Mediator
    {
        private readonly IS3Mediator _inner;
        private readonly ILogger<TracingS3Mediator> _logger;

        public TracingS3Mediator(IS3Mediator inner, ILogger<TracingS3Mediator> logger)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Uri ServiceUrl => _inner.ServiceUrl;

        public async Task Cleanup(Func<IAmazonS3, Task> action, FormattableString description)
        {
            var descriptionValue = Invariant(description);
            var timer = Stopwatch.StartNew();
            _logger.LogDebug(Events.CleaningUp, "Cleaning up {Description}.", descriptionValue);
            try
            {
                await _inner.Cleanup(action, description);
            }
            catch (OperationCanceledException exception)
            {
                _logger.LogWarning(exception, "Canceled cleaning up {Description}, elapsed {Elapsed}.", descriptionValue, timer.Elapsed.TotalMilliseconds);
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to clean up {Description}, elapsed {Elapsed}.", descriptionValue, timer.Elapsed.TotalMilliseconds);
                throw;
            }

            _logger.LogDebug(Events.CleanedUp, "Cleaned up {Description}, elapsed {Elapsed}.", descriptionValue, timer.Elapsed.TotalMilliseconds);
        }

        public async Task<TResult> Invoke<TResult>(Func<IAmazonS3, CancellationToken, Task<TResult>> action, FormattableString description, CancellationToken cancellationToken)
        {
            TResult result;
            var descriptionValue = Invariant(description);
            var timer = Stopwatch.StartNew();
            _logger.LogDebug(Events.Invoking, "Invoking {Description}.", descriptionValue);
            try
            {
                result = await _inner.Invoke(action, description, cancellationToken);
            }
            catch (OperationCanceledException exception)
            {
                _logger.LogWarning(exception, "Canceled invoking {Description}, elapsed {Elapsed}.", descriptionValue, timer.Elapsed.TotalMilliseconds);
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to invoke {Description}, elapsed {Elapsed}.", descriptionValue, timer.Elapsed.TotalMilliseconds);
                throw;
            }

            _logger.LogDebug(Events.Invoked, "Invoked {Description}, elapsed {Elapsed}.", descriptionValue, timer.Elapsed.TotalMilliseconds);
            return result;
        }

        private static class Events
        {
            public static readonly EventId CleaningUp = new(1, nameof(CleaningUp));
            public static readonly EventId CleanedUp = new(2, nameof(CleanedUp));
            public static readonly EventId Invoking = new(3, nameof(Invoking));
            public static readonly EventId Invoked = new(4, nameof(Invoked));
        }
    }
}
