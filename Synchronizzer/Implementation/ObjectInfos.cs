﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class ObjectInfos : IEnumerable<ObjectInfo>, IAsyncDisposable
    {
        private const int MaxListLength = 65536;
        private static readonly IComparer<ObjectInfo> NameOnlyComparer = new ObjectInfoNameComparer();

        private readonly IObjectSource _source;
        private readonly CancellationToken _cancellationToken;
        private List<ObjectInfo>? _infos = new();
#pragma warning disable CA2213 // Disposable fields should be disposed -- it's disposed, analyzer just doesn't understand it
        private IAsyncEnumerator<IReadOnlyCollection<ObjectInfo>>? _enumerator;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private int _skip;

        public ObjectInfos(IObjectSource source, CancellationToken cancellationToken)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _cancellationToken = cancellationToken;
        }

        public string? LastName { get; private set; }

        public bool IsLive => _infos is not null;

        public async Task Populate()
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

            _enumerator ??= _source.GetOrdered(_cancellationToken).GetAsyncEnumerator(_cancellationToken);
            if (await _enumerator.MoveNextAsync())
            {
                var index = 0;
                foreach (var info in _enumerator.Current)
                {
                    if (info.CompareTo(lastInfo) <= 0)
                    {
                        throw new InvalidDataException(FormattableString.Invariant(
                            $"Current object info {info} at index {index} is not sorted wrt {lastInfo}, last name \"{LastName}\", source {_source}."));
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

        public bool HasObject(ObjectInfo objectInfo)
        {
            if (_infos is null)
            {
                return false;
            }

            var index = _infos.BinarySearch(objectInfo, null);
            return index >= 0;
        }

        public bool HasObjectByName(ObjectInfo objectInfo)
        {
            if (_infos is null)
            {
                return false;
            }

            var index = _infos.BinarySearch(objectInfo, NameOnlyComparer);
            return index >= 0;
        }

        public override string ToString()
            => IsLive
                ? FormattableString.Invariant($"(Count={_infos?.Count};Skip={_skip};LastName={LastName})")
                : "(Completed)";

        public ValueTask DisposeAsync()
        {
            if (_enumerator is { } enumerator)
            {
                return enumerator.DisposeAsync();
            }

            return ValueTask.CompletedTask;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private static InvalidOperationException CreateCompletedException() => new("Object infos are marked as completed.");
    }
}
