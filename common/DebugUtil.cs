﻿using System;

namespace comroid.csapi.common
{
    public static class DebugUtil
    {
        [Obsolete]
        public static Action<T> WithExceptionHandler<T>(Action<Exception> handler, Action<T> action) => it =>
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

        [Obsolete]
        public static Func<TIn, TOut?> WithExceptionHandler<TIn, TOut>(Action<Exception> handler,
            Func<TIn, TOut> action) where TOut : class => WithExceptionHandler<TIn, TOut?>(e =>
        {
            handler(e);
            return null;
        }, action);

        [Obsolete]
        public static Func<TIn, TOut> WithExceptionHandler<TIn, TOut>(Func<Exception, TOut> handler,
            Func<TIn, TOut> action) => it =>
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
    }
}