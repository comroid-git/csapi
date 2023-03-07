using System;
using System.Collections.Generic;

namespace comroid.common;

public class Container : HashSet<IDisposable>, IDisposable
{
    public void Dispose()
    {
        foreach (var disposable in this) 
            disposable.Dispose();
    }
}