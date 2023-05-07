using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace comroid.common;

public class DebugWriter : TextWriter
{
    public override Encoding Encoding
    {
        get => Encoding.ASCII;
    }

    public override void Write(char[] buffer, int index, int count)
        => Debug.Write(new string(buffer)[index..(index + count)]);
}

public class MultiWriter : TextWriter
{
    public readonly HashSet<TextWriter> Writers;

    public MultiWriter(params TextWriter[] writers)
    {
        Writers = writers.ToHashSet();
    }

    public override Encoding Encoding
    {
        get => Writers.Select(x => x.Encoding).First();
    }

    public override void Write(char[] buffer, int index, int count)
    {
        foreach (var writer in Writers)
            writer.Write(buffer, index, count);
    }
}

public interface ILog
{
    private protected static readonly ConcurrentDictionary<Type, ILog> cache;
    private protected static readonly LogWriterAdapter writerAdapter;
    public static DetailLevel Detail { get; set; } =
#if DEBUG
        DetailLevel.High
#else
        DetailLevel.Low
#endif
        ;

    static ILog()
    {
        cache = new ConcurrentDictionary<Type, ILog>();
        writerAdapter = new LogWriterAdapter { Lines = TextTable.LineMode.ASCII, Header = false };
        AnsiUtil.Init();
    }

    Type Type { get; }
    string Name { get; }
    bool FullNames { get; set; }
    LogLevel Level { get; set; }
    TextWriter Writer { get; set; }
    object? At(LogLevel level, object message, Func<object, object?>? fallback = null, bool error = false);
    R? At<R>(LogLevel level, object message, Func<object, R?>? fallback = null, bool error = false);
}

internal class LogWriterAdapter : TextTable
{
    private protected static Column colTime = null!;
    private protected static Column colLevel = null!;
    private protected static Column colName = null!;
    private protected static Column colMessage = null!;

    private void InitColumns()
    {
        // late initializer due to setting of DetailLevel

        void ColTime() => colTime = AddColumn("time");
        void ColLevel() => colLevel = AddColumn("level");
        void ColName() => colName = AddColumn("name");
        void ColMessage() => colMessage = AddColumn("message");

        switch (ILog.Detail)
        {
            case DetailLevel.None:
                ColLevel();
                ColMessage();
                break;
            case DetailLevel.Low:
                ColLevel();
                ColName();
                ColMessage();
                break;
            default:
                ColTime();
                ColLevel();
                ColName();
                ColMessage();
                break;
        }
    }

    public void WriteLine(Row row, TextWriter writer = null!)
    {
        writer ??= Console.Out;
        var cc = Columns.Count;
        if (cc == 0)
            InitColumns();
        if (cc != (colLengths?.Length ?? -1))
        {
            (var old, colLengths) = (colLengths, new int[cc]);
            Array.Copy(old ?? Array.Empty<int>(), colLengths, old?.Length ?? 0);
        }

        var sb = new StringBuilder();
        var indent = VertIndent(LineType.IdxHorizontal);
        for (var i = 0; i < cc; i++)
        {
            // for each column, collect longest data
            var col = Columns[i];
            var text = row._data[col.Name].ToString()!;
            var len = text.Length;
            if (colLengths![i] < len)
                colLengths[i] = Math.Min(len, 64);

            // and then write column with updated lengths
            var lines = Lines != null;
            if (lines && row.Separator)
            {
                var totalW = colLengths.Aggregate(0, (a, b) => a + b + margin /**/) + cc + colLengths.Length * margin;
                var colTrims = colLengths.Select(x => 1 + x + margin * 2).Append(0).ToArray();
                sb.Append(HoriDetailLine(totalW, colTrims, LineType.IdxVertical, row.Detail));
                continue;
            }

            sb.Append(text.Adjust(Math.Max(colLengths[i], text.Length), col._justifyRight));
            if (i < cc - 1)
                if (lines) sb.Append(indent);
                else sb.Append(' ');
        }

        writer.WriteLine(sb);
    }
}

public class LogLevelTextWriter : TextWriter
{
    private readonly StringBuilder buffer = new();
    private readonly LogLevel level;
    private readonly Log log;

    public LogLevelTextWriter(Log log, LogLevel level)
    {
        this.log = log;
        this.level = level;
    }

    public override Encoding Encoding
    {
        get => Encoding.ASCII;
    }

    public override void Write(char value) => buffer.Append(value);

    public override void Flush()
    {
        log.At(level, buffer);
        buffer.Clear();
    }
}

public class Log : ILog
{
    private const LogLevel UnsetLevel = unchecked((LogLevel)(-1));
    public const LogLevel DefaultLevel =
#if DEBUG
            LogLevel.Trace
#else
            LogLevel.Info
#endif
        ;
    private const DetailLevel UnsetDetail = unchecked((DetailLevel)(-1));
    public static readonly Log Root = new("Root");
    public static readonly Log Debug = new(Root, typeof(Debug), "Debug") { Writer = new DebugWriter() };
    private readonly ILog? _parent;
    private bool? _fullNames;
    private LogLevel? _level = UnsetLevel;
    private DetailLevel? _detail = UnsetDetail;
    private string? _name;
    private TextWriter? _writer;

    public Log() : this(null!)
    {
    }

    public Log(string name) : this(typeof(Log), name)
    {
    }

    public Log(Type type, string? name = null) : this(Root, type, name)
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
        get => _fullNames ?? _parent?.FullNames ?? false;
        set => _fullNames = value;
    }

    public LogLevel Level
    {
        get => _level == UnsetLevel ? DefaultLevel : _level ?? _parent?.Level ?? DefaultLevel;
        set => _level = value;
    }

    public TextWriter Writer
    {
        get => _writer ?? _parent?.Writer ?? Console.Out;
        set => _writer = value;
    }

    public TextWriter CreateWriter(LogLevel level) => new LogLevelTextWriter(this, level);

    public object? At(LogLevel level, object? message, Func<object, object?>? fallback = null, bool error = false) => _Log(level, message, fallback, error);

    public R? At<R>(LogLevel level, object? message, Func<object, R?>? fallback = null, bool error = false) => _Log(level, message, fallback, error);

    private R? _Log<R>(LogLevel level, object? message, Func<object, R?>? fallback, bool error)
    {
        message ??= string.Empty;
        var fb = _FB(message, fallback);
        if (Level < level)
            return fb;
        ILog.writerAdapter.WriteLine(new TextTable.Row
        {
            _data =
            {
                { "time", DateTime.Now },
                { "level", _LV(level) },
                { "name", Name },
                { "message", message }
            }
        }, Writer);
        if (error) throw new Exception(message.ToString());
        return fb;
    }

    private R? _FB<R>(object message, Func<object, R?>? fallback)
    {
        return RunWithExceptionLogger(() => fallback != null ? fallback(message) ?? default : (R?)(object)null!);
    }

    private string _LV(LogLevel level)
    {
        var str = string.Empty;
        var useAnsi = Writer == Console.Out && AnsiUtil.Enabled;
        if (useAnsi)
            str += level switch
            {
                LogLevel.Trace => AnsiUtil.BrightMagenta,
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

    private string _PE(Exception e, LogLevel exceptionLevel, bool? printException) => printException ?? exceptionLevel <= LogLevel.Error ? "\r\n" + e : string.Empty;

    public Func<Exception, object?> ExceptionLogger(object? message = null,
        LogLevel exceptionLevel = LogLevel.Fatal, Func<object, object?>? fallback = null,
        bool? printException = null)
    {
        return e => At(exceptionLevel,
            (message ?? $"{e.GetType().Name}: {e.Message}") + _PE(e, exceptionLevel, printException), fallback);
    }

    public Func<Exception, R?> ExceptionLogger<R>(object? message = null,
        LogLevel exceptionLevel = LogLevel.Fatal, Func<object, R?>? fallback = null,
        bool? printException = null)
    {
        return e => At(exceptionLevel,
            (message ?? $"{e.GetType().Name}: {e.Message}") + _PE(e, exceptionLevel, printException), fallback);
    }

    #region Callable Wrapping

    public Action WrapWithExceptionLogger(Action action,
        string? message = null,
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
        string? message = null,
        LogLevel exceptionLevel = LogLevel.Fatal,
        bool? printException = null)
    {
        return t =>
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

    public Func<object?> WrapWithExceptionLogger(Func<object?> action, string? message = null,
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

    public Func<R?> WrapWithExceptionLogger<R>(Func<R?> action, string? message = null,
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

    public void RunWithExceptionLogger(Action action, string? message = null,
        LogLevel exceptionLevel = LogLevel.Fatal,
        bool? printException = null)
    {
        WrapWithExceptionLogger(action, message, exceptionLevel, printException)();
    }

    public object? RunWithExceptionLogger(Func<object?> action, string? message = null,
        Func<object, object?>? fallback = null,
        LogLevel exceptionLevel = LogLevel.Fatal,
        bool? printException = null) => WrapWithExceptionLogger(action, message, exceptionLevel, fallback, printException)();

    public R? RunWithExceptionLogger<R>(Func<R?> action, string? message = null,
        Func<object, R?>? fallback = null,
        LogLevel exceptionLevel = LogLevel.Fatal,
        bool? printException = null) => WrapWithExceptionLogger(action, message, exceptionLevel, fallback, printException)();

    #endregion
}

public class Log<T> : Log where T : class
{
    public Log(Type type) : base(Root, type, null)
    {
    }

    public Log(ILog parent, Type type) : base(parent, type, null)
    {
    }

    public override string Name
    {
        get => FullNames ? Type.FullName! : Type.Name;
    }

    public new static object? At(LogLevel level, object message, Func<object, object?>? fallback = null,
        bool error = false) => ((Log)Get()).At(level, message, fallback, error);

    public new static R? At<R>(LogLevel level, object message, Func<object, R?>? fallback = null, bool error = false) => ((Log)Get()).At(level, message, fallback, error);

    public static Log<T> Get()
    {
        return (Log<T>)ILog.cache.GetOrAdd(typeof(T), t => new Log<T>(t));
    }
}

public enum LogLevel : byte
{
    None = 0,
    Fatal = 1,
    Error = 42,
    Warning = 85,
    Info = 127,
    Config = 170,
    Debug = 212,
    Trace = 254,
    All = 255
}

public enum DetailLevel : byte
{
    None = 0, // effectively Console.WriteLine() with log level
    Low = 1, // also includes short logger name
    Medium = 2, // also includes time
    High = 3, // also includes date
    Extreme = 4, // includes long logger names
    Trimmed = 5 // TODO trims excessive class names
}