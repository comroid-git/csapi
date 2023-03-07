using System.Collections.Concurrent;
using comroid.common;

namespace comroid.textUI;

[Flags]
public enum UICapability : ushort
{
    None = 0x0000,
    
    Read = 0x0001,
    Write = 0x0002,
    LF = 0x0004,
    CRLF = 0x0008,
    
    Unicode = 0x0010,
    TrimWhitespace = 0x0020,

    AnsiColorizable = 0x0100,
    AnsiScrollable = 0x0200,
    
    MarkdownDecoration = 0x1000,
    MarkdownTables = 0x2000,
    MarkdownHeadlines = 0x4000,
    MarkdownURLs = 0x8000
}

public abstract class UIModule
{
    private const string ErrorMsg_ReqChl = "UIModule requires a channel parameter to be set";
    private readonly ConcurrentDictionary<object, Guid> _channels = new();

    public virtual bool UsesChannels => false;
    public virtual UICapability Capabilities => UICapability.Write | UICapability.Read;

    public virtual string? WaitForInput(Guid? channel, object? message = null)
        => WaitForInputAsync(channel, message).Await();
    public abstract Task<string?> WaitForInputAsync(Guid? channel, object? message = null);
    public abstract void WriteOutput(Guid? channel, object message);
    
    public virtual string? WaitForInput(object? message = null) => UsesChannels ? throw new NotSupportedException(ErrorMsg_ReqChl) : WaitForInput(null, message);
    public virtual void WriteOutput(object message)
    {
        if (UsesChannels)
            throw new NotSupportedException(ErrorMsg_ReqChl);
        else WriteOutput(null, message);
    }

    public IEnumerable<object> Keys => _channels.Keys;
    public IEnumerable<Guid> IDs => _channels.Values;
    public IEnumerable<(Guid, object)> Channels => _channels.Values.Select(id => (id, GetChannelKey(id)!));
    public Guid RegisterChannel(object channel, Guid? guid = null) => _channels.GetOrAdd(PreProcessChannel(channel), _ => guid ?? Guid.NewGuid());
    public object? GetChannelKey(Guid channel) => _channels.FirstOrDefault(entry => entry.Value == channel);
    public R? GetChannelKey<R>(Guid channel) => (R?)GetChannelKey(channel);

    public bool HasCapabilities(params UICapability[] caps) => caps.All(c => (Capabilities & c) != 0);
    protected virtual object PreProcessChannel(object channel) => channel;
    protected virtual string PreProcessMessage(object? message) => message?.ToString()
        ?.Cleanup(!HasCapabilities(UICapability.LF) | HasCapabilities(UICapability.CRLF),
            !HasCapabilities(UICapability.AnsiColorizable, UICapability.AnsiScrollable),
            HasCapabilities(UICapability.TrimWhitespace)) ?? string.Empty;
}