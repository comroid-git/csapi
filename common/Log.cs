using System;
using System.Collections.Concurrent;
using System.IO;

namespace comroid.csapi.common;

public interface ILog
{
    private protected static readonly ConcurrentDictionary<Type, ILog> cache;
    private protected static readonly TextTable writerAdapter;
    private protected static readonly TextTable.Column colTime;
    private protected static readonly TextTable.Column colLevel;
    private protected static readonly TextTable.Column colName;
    private protected static readonly TextTable.Column colMessage;
    public static readonly Log<ILog> BaseLogger;

    static ILog()
    {
        AnsiUtil.Init();
        cache = new ConcurrentDictionary<Type, ILog>();
        writerAdapter = new TextTable { Lines = TextTable.LineMode.ASCII, Header = false };
        colTime = writerAdapter.AddColumn("time");
        colLevel = writerAdapter.AddColumn("level");
        colName = writerAdapter.AddColumn("name");
        colMessage = writerAdapter.AddColumn("message");
        BaseLogger = new Log<ILog>(null!, typeof(ILog));
    }

    Type Type { get; }
    bool FullNames { get; set; }
    LogLevel Level { get; set; }
    TextWriter Writer { get; set; }
    object? Write(LogLevel level, object message, Func<object, object?>? fallback = null, bool error = false);
    R? Write<R>(LogLevel level, object message, Func<object, R?>? fallback = null, bool error = false);
}

public class Log<T> : ILog where T : class
{
    private readonly ILog? _parent;
    private bool? _fullNames;
    private LogLevel? _level;
    private TextWriter? _writer;

    public Log(Type type) : this(ILog.BaseLogger, type)
    {
    }

    public Log(ILog parent, Type type
    )
    {
        _parent = parent;
        Type = type;
    }

    public Type Type { get; }

    public bool FullNames
    {
        get => _fullNames ?? _parent?.FullNames ?? true;
        set => _fullNames = value;
    }

    public LogLevel Level
    {
        get => _level ?? _parent?.Level ??
#if DEBUG
            LogLevel.Debug
#else
            LogLevel.Info
#endif
        ;
        set => _level = value;
    }

    public TextWriter Writer
    {
        get => _writer ?? _parent?.Writer ?? Console.Out;
        set => _writer = value;
    }

    public object? Write(LogLevel level, object message, Func<object, object?>? fallback = null, bool error = false)
    {
        return _Log(level, message, fallback, error);
    }

    public R? Write<R>(LogLevel level, object message, Func<object, R?>? fallback = null, bool error = false)
    {
        return _Log(level, message, fallback, error);
    }

    public static object? At(LogLevel level, object message, Func<object, object?>? fallback = null, bool error = false)
    {
        return Get().Write(level, message, fallback, error);
    }

    public static R? At<R>(LogLevel level, object message, Func<object, R?>? fallback = null, bool error = false)
    {
        return Get().Write(level, message, fallback, error);
    }

    public static Func<Exception, object?> ExceptionLogger(object message, Func<object, object?>? fallback = null,
        LogLevel exceptionLevel = LogLevel.Fatal)
    {
        return e => Get().Write(exceptionLevel, message.ToString() + '\n' + e, fallback);
    }

    public static Func<Exception, R?> ExceptionLogger<R>(object message, Func<object, R?>? fallback = null,
        LogLevel exceptionLevel = LogLevel.Fatal)
    {
        return e => Get().Write(exceptionLevel, message.ToString() + '\n' + e, fallback);
    }

    public static void WithExceptionLogger(Action action, string message = "Unhandled Internal Exception",
        LogLevel exceptionLevel = LogLevel.Fatal)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            ExceptionLogger(message, exceptionLevel: exceptionLevel)(e);
        }
    }

    public static object? WithExceptionLogger(Func<object?> action, string message = "Unhandled Internal Exception",
        Func<object, object?>? fallback = null,
        LogLevel exceptionLevel = LogLevel.Fatal)
    {
        try
        {
            return action();
        }
        catch (Exception e)
        {
            return ExceptionLogger(message, fallback, exceptionLevel)(e);
        }
    }

    public static R? WithExceptionLogger<R>(Func<R?> action, string message = "Unhandled Internal Exception",
        Func<object, R?>? fallback = null,
        LogLevel exceptionLevel = LogLevel.Fatal)
    {
        try
        {
            return action();
        }
        catch (Exception e)
        {
            return ExceptionLogger(message, fallback, exceptionLevel)(e);
        }
    }

    public static Log<T> Get()
    {
        return (Log<T>)ILog.cache.GetOrAdd(typeof(T), t => new Log<T>(t));
    }

    private R? _Log<R>(LogLevel level, object message, Func<object, R?>? fallback, bool error)
    {
        if (Level < level)
            return _FB(message, fallback);
        ILog.writerAdapter.WriteLine(new TextTable.Row
        {
            _data =
            {
                { ILog.colTime, DateTime.Now },
                { ILog.colLevel, _LV(level) },
                { ILog.colName, (FullNames ? Type.FullName : Type.Name)! },
                { ILog.colMessage, message }
            }
        }, Writer);
        if (error) throw new Exception(message.ToString());
        return _FB(message, fallback);
    }

    private R? _FB<R>(object message, Func<object, R?>? fallback)
    {
        return fallback != null ? fallback(message) ?? default : (R?)(object)null!;
    }

    private string _LV(LogLevel level)
    {
        var str = string.Empty;
        var useAnsi = Writer == Console.Out && AnsiUtil.Enabled;
        if (useAnsi)
            str += level switch
            {
                LogLevel.Trace => AnsiUtil.BackgroundWhite,
                LogLevel.Debug => AnsiUtil.BackgroundBrightWhite,
                LogLevel.Config => AnsiUtil.Blue,
                LogLevel.Info => AnsiUtil.Green,
                LogLevel.Warning => AnsiUtil.Yellow,
                LogLevel.Error => AnsiUtil.BrightRed,
                LogLevel.Fatal => AnsiUtil.Red,
                _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
            };
        str += level.ToString();
        if (useAnsi)
            str += AnsiUtil.Reset;
        return str;
    }
}

public enum LogLevel : byte
{
    Trace = 255,
    Debug = 212,
    Config = 170,
    Info = 127,
    Warning = 85,
    Error = 42,
    Fatal = 0
}