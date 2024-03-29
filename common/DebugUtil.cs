﻿using System;
using System.Collections.Generic;
using System.IO;
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

    public static Version GetAssemblyVersion<T>() => typeof(T).Assembly.GetName().Version!;

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

    public static bool TouchFile(string path) => TouchFile(path, out _, out _, true);
    public static bool TouchFile(string path, out Exception? e) => TouchFile(path, out _, out e, true);
    public static bool TouchFile(string path, out FileStream? fs, FileMode mode = FileMode.OpenOrCreate) =>
        TouchFile(path, out fs, out _, mode: mode);
    public static bool TouchFile(string path, out FileStream? fs, out Exception? e, bool dispose = false,
        FileMode mode = FileMode.OpenOrCreate)
    {
        e = null;
        fs = null;
        if (File.Exists(path))
            return dispose || (fs = File.Open(path, mode)) != null;
        try
        {
            fs = File.Create(path);
            fs.Flush();
            return true;
        }
        catch (Exception ex)
        {
            e = ex;
            return fs != null;
        }
        finally
        {
            if (dispose)
                fs?.Dispose();
        }
    }
}