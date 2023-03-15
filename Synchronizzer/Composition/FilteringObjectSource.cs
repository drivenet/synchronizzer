using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;

using Synchronizzer.Implementation;

namespace Synchronizzer.Composition;

internal sealed class FilteringObjectSource : IObjectSource
{
    private readonly IObjectSource _inner;
    private readonly Regex _exclude;

    public FilteringObjectSource(IObjectSource inner, Regex exclude)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _exclude = exclude ?? throw new ArgumentNullException(nameof(exclude));
    }

    public async IAsyncEnumerable<IReadOnlyCollection<ObjectInfo>> GetOrdered(bool nice, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        List<ObjectInfo>? filteredInfosBuffer = null;
        await foreach (var infos in _inner.GetOrdered(nice, cancellationToken))
        {
            var result = infos;
            var processed = 0;
            List<ObjectInfo>? filteredInfos = null;
            foreach (var info in infos)
            {
                if (!info.IsHidden
                    && _exclude.IsMatch(info.Name))
                {
                    if (filteredInfos is null)
                    {
                        if (filteredInfosBuffer is null)
                        {
                            filteredInfosBuffer = new();
                        }
                        else
                        {
                            filteredInfosBuffer.Clear();
                        }

                        result = filteredInfos = filteredInfosBuffer;
                        filteredInfos.AddRange(infos.Take(processed));
                    }

                    filteredInfos.Add(new(info.Name, info.Size, true, info.Timestamp));
                }
                else
                {
                    filteredInfos?.Add(info);
                }

                ++processed;
            }

            yield return result;
        }
    }
}
