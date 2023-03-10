using System;
using System.Collections.Generic;

namespace comroid.common;

public class Container<T> : HashSet<T>, IDisposable where T : IDisposable
{
    public virtual void Dispose()
    {
        foreach (var disposable in this)
            disposable.Dispose();
    }
}

public class Container : Container<IDisposable>
{
}
