using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace comroid.common;

public static class DebugUtil
{
    public static readonly Random RNG = new();

    public static Dictionary<string, T?> GetConstantsOfClass<T>(Type of)
    {
        return of.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(entry => entry.FieldType == typeof(T))
            .ToDictionary(entry => entry.Name, entry => (T)entry.GetValue(null)!)!;
    }

    [Obsolete]
    public static Action<T> WithExceptionHandler<T>(Action<Exception> handler, Action<T> action)
    {
        return it =>
        {
            try
            {
                action(it);
            }
            catch (Exception e)
            {
                handler(e);
            }
        };
    }

    [Obsolete]
    public static Func<TIn, TOut?> WithExceptionHandler<TIn, TOut>(Action<Exception> handler,
        Func<TIn, TOut> action) where TOut : class
    {
        return WithExceptionHandler<TIn, TOut?>(e =>
        {
            handler(e);
            return null;
        }, action);
    }

    [Obsolete]
    public static Func<TIn, TOut> WithExceptionHandler<TIn, TOut>(Func<Exception, TOut> handler,
        Func<TIn, TOut> action)
    {
        return it =>
        {
            try
            {
                return action(it);
            }
            catch (Exception e)
            {
                return handler(e);
            }
        };
    }

    public static Version GetAssemblyVersion<T>()
    {
        return typeof(T).Assembly.GetName().Version!;
    }

    public static long Measure(Action action)
    {
        var time = UnixTime();
        action();
        return UnixTime() - time;
    }

    public static long UnixTime() // oops its actually nanoseconds
    {
        var epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (DateTime.UtcNow - epochStart).Ticks / 10;
    }
}