using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Synchronizzer.Implementation
{
    internal sealed class ObjectInfos : IEnumerable<ObjectInfo>, IAsyncDisposable
    {
        private const int MaxListLength = 65536;
        private static readonly IComparer<ObjectInfo> MetadataComparer = new ObjectInfoMetadataComparer();
        private static readonly IComparer<ObjectInfo> NameComparer = new ObjectInfoNameComparer();

        private readonly IObjectSource _source;
        private readonly CancellationToken _cancellationToken;
        private readonly ILogger? _logger;
        private List<ObjectInfo>? _infos = new();
#pragma warning disable CA2213 // Disposable fields should be disposed -- it's disposed, analyzer just doesn't understand it
        private IAsyncEnumerator<IReadOnlyList<ObjectInfo>>? _enumerator;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private ValueTask<bool> _enumerationTask;
        private int _skip;

        public ObjectInfos(IObjectSource source, ILogger<ObjectInfos>? logger, CancellationToken cancellationToken)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _logger = logger;
            _cancellationToken = cancellationToken;
        }

        public string? LastName { get; private set; }

        public bool IsLive => _infos is not null;

        public async Task Populate(bool nice)
        {
            if (_infos is null)
            {
                return;
            }

            var lastInfo = _infos.LastOrDefault();
            _infos.RemoveRange(0, _skip);
            _skip = 0;
            if (_infos.Count >= MaxListLength)
            {
                return;
            }

            if (_enumerator is null)
            {
                var enumerator = _source.GetOrdered(nice, _cancellationToken).GetAsyncEnumerator(_cancellationToken);
#pragma warning disable CA2012 // Use ValueTasks correctly -- the _enumerationTask is awaited only once
                _enumerationTask = enumerator.MoveNextAsync();
#pragma warning restore CA2012 // Use ValueTasks correctly
                _enumerator = enumerator;
            }

            if (await _enumerationTask)
            {
                IReadOnlyList<ObjectInfo> infos;
                try
                {
                    infos = _enumerator.Current;
                }
                finally
                {
#pragma warning disable CA2012 // Use ValueTasks correctly -- the _enumerationTask is awaited only once
                    _enumerationTask = _enumerator.MoveNextAsync();
#pragma warning restore CA2012 // Use ValueTasks correctly
                }

                var index = 0;
                foreach (var info in infos)
                {
                    if (info.CompareTo(lastInfo) <= 0)
                    {
                        if (_logger is { } logger)
                        {
                            foreach (var chunk in infos.Chunk(200))
                            {
                                logger.LogWarning("{Chunk}", string.Join("\n", chunk.Select(info => info.ToString())));
                            }
                        }

                        throw new InvalidDataException(FormattableString.Invariant(
                            $"Current object info {info} at index {index} is not sorted wrt {lastInfo}, source {_source}, see logs for more info."));
                    }

                    _infos.Add(info);
                    lastInfo = info;
                    ++index;
                    LastName = info.Name;
                }
            }

            var count = _infos.Count;
            if (count == 0)
            {
                _infos = null;
                LastName = null;
            }
        }

        public IEnumerator<ObjectInfo> GetEnumerator() => (_infos ?? Enumerable.Empty<ObjectInfo>()).GetEnumerator();

        public void Skip()
        {
            if (_infos is null)
            {
                throw CreateCompletedException();
            }

            var count = _infos.Count;
            if (_skip >= count)
            {
                throw new InvalidOperationException(FormattableString.Invariant($"Skip {_skip} is greater than or equal to count {count}."));
            }

            ++_skip;
        }

        public ObjectInfo? FindObjectByMetadata(ObjectInfo objectInfo)
        {
            if (_infos is null)
            {
                return null;
            }

            var index = _infos.BinarySearch(objectInfo, MetadataComparer);
            if (index < 0)
            {
                return null;
            }

            return _infos[index];
        }

        public bool HasObjectByName(ObjectInfo objectInfo)
        {
            if (_infos is null)
            {
                return false;
            }

            var index = _infos.BinarySearch(objectInfo, NameComparer);
            if (index < 0)
            {
                return false;
            }

            return !_infos[index].IsHidden;
        }

        public override string ToString()
            => IsLive
                ? FormattableString.Invariant($"(Count={_infos?.Count};Skip={_skip};LastName={LastName})")
                : "(Completed)";

        public ValueTask DisposeAsync()
        {
            if (_enumerator is { } enumerator)
            {
                try
                {
                    return enumerator.DisposeAsync();
                }
                catch (NotSupportedException)
                {
                    // Thrown when the enumerator task is not yet completed
                }
            }

            return ValueTask.CompletedTask;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private static InvalidOperationException CreateCompletedException() => new("Object infos are marked as completed.");
    }
}
