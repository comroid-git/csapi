using System;

namespace comroid.csapi.common
{
    public static class DebugUtil
    {
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

        public static Func<TIn, TOut?> WithExceptionHandler<TIn, TOut>(Action<Exception> handler,
            Func<TIn, TOut> action) where TOut : class => WithExceptionHandler<TIn, TOut?>(e =>
        {
            handler(e);
            return null;
        }, action);

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
    }
}