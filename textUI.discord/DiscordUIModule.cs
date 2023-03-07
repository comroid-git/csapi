using System.Text;
using comroid.common;
using Discord;
using Discord.Commands;
using Discord.Commands.Builders;
using Discord.Rest;
using Discord.WebSocket;

namespace comroid.textUI.discord;

public class DiscordUIModule : UIModule
{
    public override bool UsesChannels => true;
    public override UICapability Capabilities => UICapability.Write | UICapability.Unicode | UICapability.MarkdownDecoration;
    public readonly IDiscordClient Client;

    public DiscordUIModule(IDiscordClient client)
    {
        Client = client;
    }

    private Task<string?> WaitForInputCommandUse(Guid? channel)
    {
        throw new NotImplementedException();
    }

    public override async Task<string?> WaitForInputAsync(Guid? channel, object? message = null)
    {
        var id = GetChannelKey<ulong>(channel ?? throw new ArgumentNullException(nameof(channel)));
        var chl = (IMessageChannel)await Client.GetChannelAsync(id);
        if (message != null)
            chl.SendMessageAsync(message.ToString()).Wait();
        return await WaitForInputCommandUse(channel);
    }

    public override void WriteOutput(Guid? channel, object message)
    {
        var id = GetChannelKey<ulong>(channel ?? throw new ArgumentNullException(nameof(channel)));
        var chl = (IMessageChannel)Client.GetChannelAsync(id).Await();
        chl.SendMessageAsync(message.ToString()).Wait();
    }

    protected override object PreProcessChannel(object channel)
    {
        return channel switch
        {
            ulong id => id,
            IChannel chl => chl.Id,
            _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, "Argument is of invalid type")
        };
    }
}
