using comroid.textUI;
using SFML.Window;

namespace textUI.sfml;

public class SFMLUIModule : UIModule
{
    private readonly Window _window;
    public override UICapability Capabilities => UICapability.None;

    public SFMLUIModule(Window window)
    {
        _window = window;
    }

    public override Task<string?> WaitForInputAsync(Guid? channel, object? message = null)
    {
        throw new NotImplementedException();
    }

    public override void WriteOutput(Guid? channel, object message)
    {
        throw new NotImplementedException();
    }
}