using System;
using System.Collections.Generic;

namespace Synchronizzer.Implementation;

internal sealed class CompositeDisposable : IDisposable
{
    private readonly IEnumerable<IDisposable> _disposables;

    public CompositeDisposable(IEnumerable<IDisposable> disposables)
    {
        _disposables = disposables ?? throw new ArgumentNullException(nameof(disposables));
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
    }
}
