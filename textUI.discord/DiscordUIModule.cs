using System.Text;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace comroid.textUI.discord;

public class DiscordUIModule : UIModule
{
    public override bool UsesChannels => true;
    public readonly IDiscordClient Client = new DiscordSocketClient();

    public override string? WaitForInput(Guid? channel, string? message = null)
    {
        var chl = GetChannelKey<ulong>(channel ?? throw new ArgumentNullException(nameof(channel)));
        Client.GetChannelAsync(chl)
    }

    public override void WriteOutput(Guid? channel, object message)
    {
        throw new NotImplementedException();
    }
}
