using System;
using System.Collections.Concurrent;
using System.IO;

namespace comroid.csapi.common;

public interface ILog
{
    private protected const string DEFAULT_ERROR_MESSAGE = "Unhandled internal Exception";
    private protected static readonly ConcurrentDictionary<Type, ILog> cache;
    private protected static readonly TextTable writerAdapter;
    private protected static readonly TextTable.Column colTime;
    private protected static readonly TextTable.Column colLevel;
    private protected static readonly TextTable.Column colName;
    private protected static readonly TextTable.Column colMessage;

    static ILog()
    {
        cache = new ConcurrentDictionary<Type, ILog>();
        writerAdapter = new TextTable { Lines = TextTable.LineMode.ASCII, Header = false };
        colTime = writerAdapter.AddColumn("time");
        colLevel = writerAdapter.AddColumn("level");
        colName = writerAdapter.AddColumn("name");
        colMessage = writerAdapter.AddColumn("message");
        AnsiUtil.Init();
    }

    public static Log BaseLogger => Log.BaseLogger;

    Type Type { get; }
    string Name { get; }
    bool FullNames { get; set; }
    LogLevel Level { get; set; }
    TextWriter Writer { get; set; }
    object? At(LogLevel level, object message, Func<object, object?>? fallback = null, bool error = false);
    R? At<R>(LogLevel level, object message, Func<object, R?>? fallback = null, bool error = false);
}

public class Log : ILog
{
    public static readonly Log BaseLogger = new();
    private readonly ILog? _parent;
    private bool? _fullNames;
    private LogLevel? _level;
    private string? _name;
    private TextWriter? _writer;

    public Log() : this(null!)
    {
    }

    public Log(string name) : this(typeof(Log), name)
    {
    }

    public Log(Type type, string? name = null) : this(ILog.BaseLogger, type, name)
    {
    }

    public Log(ILog parent, Type type, string? name)
    {
        _parent = parent;
        Type = type;
        _name = name;
    }

    public Type Type { get; }

    public virtual string Name
    {
        get => _name ?? (FullNames ? Type.FullName : Type.Name) ?? "Logger";
        set => _name = value;
    }

    public bool FullNames
    {
        get => _fullNames ?? _parent?.FullNames ?? true;
        set => _fullNames = value;
    }

    public LogLevel Level
    {
        get => _level ?? _parent?.Level ??
#if DEBUG
            LogLevel.Trace
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

    public object? At(LogLevel level, object message, Func<object, object?>? fallback = null, bool error = false)
    {
        return _Log(level, message, fallback, error);
    }

    public R? At<R>(LogLevel level, object message, Func<object, R?>? fallback = null, bool error = false)
    {
        return _Log(level, message, fallback, error);
    }

    private R? _Log<R>(LogLevel level, object message, Func<object, R?>? fallback, bool error)
    {
        var fb = _FB(message, fallback);
        if (Level < level)
            return fb;
        ILog.writerAdapter.WriteLine(new TextTable.Row
        {
            _data =
            {
                { ILog.colTime, DateTime.Now },
                { ILog.colLevel, _LV(level) },
                { ILog.colName, Name },
                { ILog.colMessage, message }
            }
        }, Writer);
        if (error) throw new Exception(message.ToString());
        return fb;
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
                LogLevel.Trace => AnsiUtil.BrightBlue,
                LogLevel.Debug => AnsiUtil.BrightGreen,
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

    private string _PE(Exception e, LogLevel exceptionLevel, bool? printException)
    {
        return printException ?? exceptionLevel <= LogLevel.Error ? "\r\n" + e : string.Empty;
    }

    public Func<Exception, object?> ExceptionLogger(object message,
        LogLevel exceptionLevel = LogLevel.Fatal, Func<object, object?>? fallback = null,
        bool? printException = null)
    {
        return e => At(exceptionLevel, message + _PE(e, exceptionLevel, printException), fallback);
    }

    public Func<Exception, R?> ExceptionLogger<R>(object message,
        LogLevel exceptionLevel = LogLevel.Fatal, Func<object, R?>? fallback = null,
        bool? printException = null)
    {
        return e => At(exceptionLevel, message + _PE(e, exceptionLevel, printException), fallback);
    }

    #region Callable Wrapping

    public Action WrapWithExceptionLogger(Action action,
        string message = ILog.DEFAULT_ERROR_MESSAGE,
        LogLevel exceptionLevel = LogLevel.Fatal,
        bool? printException = null)
    {
        return () =>
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                ExceptionLogger(message, exceptionLevel, printException: printException)(e);
            }
        };
    }
    public Action<T> WrapWithExceptionLogger<T>(Action<T> action,
        string message = ILog.DEFAULT_ERROR_MESSAGE,
        LogLevel exceptionLevel = LogLevel.Fatal,
        bool? printException = null)
    {
        return (t) =>
        {
            try
            {
                action(t);
            }
            catch (Exception e)
            {
                ExceptionLogger(message, exceptionLevel, printException: printException)(e);
            }
        };
    }

    public Func<object?> WrapWithExceptionLogger(Func<object?> action, string message = ILog.DEFAULT_ERROR_MESSAGE,
        LogLevel exceptionLevel = LogLevel.Fatal,
        Func<object, object?>? fallback = null,
        bool? printException = null)
    {
        return () =>
        {
            try
            {
                return action();
            }
            catch (Exception e)
            {
                return ExceptionLogger(message, exceptionLevel, fallback, printException)(e);
            }
        };
    }

    public Func<R?> WrapWithExceptionLogger<R>(Func<R?> action, string message = ILog.DEFAULT_ERROR_MESSAGE,
        LogLevel exceptionLevel = LogLevel.Fatal,
        Func<object, R?>? fallback = null,
        bool? printException = null)
    {
        return () =>
        {
            try
            {
                return action();
            }
            catch (Exception e)
            {
                return ExceptionLogger(message, exceptionLevel, fallback, printException)(e);
            }
        };
    }

    public void RunWithExceptionLogger(Action action, string message = ILog.DEFAULT_ERROR_MESSAGE,
        LogLevel exceptionLevel = LogLevel.Fatal,
        bool? printException = null)
    {
        WrapWithExceptionLogger(action, message, exceptionLevel, printException)();
    }

    public object? RunWithExceptionLogger(Func<object?> action, string message = ILog.DEFAULT_ERROR_MESSAGE,
        Func<object, object?>? fallback = null,
        LogLevel exceptionLevel = LogLevel.Fatal,
        bool? printException = null)
    {
        return WrapWithExceptionLogger(action, message, exceptionLevel, fallback, printException)();
    }

    public R? RunWithExceptionLogger<R>(Func<R?> action, string message = ILog.DEFAULT_ERROR_MESSAGE,
        Func<object, R?>? fallback = null,
        LogLevel exceptionLevel = LogLevel.Fatal,
        bool? printException = null)

    {
        return WrapWithExceptionLogger(action, message, exceptionLevel, fallback, printException)();
    }

    #endregion
}

public class Log<T> : Log where T : class
{
    public Log(Type type) : base(ILog.BaseLogger, type, null)
    {
    }

    public Log(ILog parent, Type type) : base(parent, type, null)
    {
    }

    public override string Name => FullNames ? Type.FullName! : Type.Name;

    public new static object? At(LogLevel level, object message, Func<object, object?>? fallback = null,
        bool error = false)
    {
        return ((Log)Get()).At(level, message, fallback, error);
    }

    public new static R? At<R>(LogLevel level, object message, Func<object, R?>? fallback = null, bool error = false)
    {
        return ((Log)Get()).At(level, message, fallback, error);
    }

    public static Log<T> Get()
    {
        return (Log<T>)ILog.cache.GetOrAdd(typeof(T), t => new Log<T>(t));
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