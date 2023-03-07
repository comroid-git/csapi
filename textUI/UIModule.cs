using System.Collections.Concurrent;

namespace comroid.textUI;

public abstract class UIModule
{
    private const string ErrorMsg_ReqChl = "UIModule requires a channel parameter to be set";
    private readonly ConcurrentDictionary<object, Guid> _channels = new();

    public virtual bool UsesChannels => false;
    public abstract string? WaitForInput(Guid? channel, string? message = null);
    public abstract void WriteOutput(Guid? channel, object message);
    
    public string? WaitForInput(string? message = null) => UsesChannels ? throw new NotSupportedException(ErrorMsg_ReqChl) : WaitForInput(null, message);
    public void WriteOutput(object message)
    {
        if (UsesChannels)
            throw new NotSupportedException(ErrorMsg_ReqChl);
        else WriteOutput(null, message);
    }

    public IEnumerable<object> Keys => _channels.Keys;
    public IEnumerable<Guid> IDs => _channels.Values;
    public IEnumerable<(Guid, object)> Channels => _channels.Values.Select(id => (id, GetChannelKey(id)!));
    public Guid RegisterChannel(object channel, Guid? guid = null) => _channels.GetOrAdd(channel, _ => guid ?? Guid.NewGuid());
    public object? GetChannelKey(Guid channel) => _channels.FirstOrDefault(entry => entry.Value == channel);
    public R? GetChannelKey<R>(Guid channel) => (R?)GetChannelKey(channel);
}