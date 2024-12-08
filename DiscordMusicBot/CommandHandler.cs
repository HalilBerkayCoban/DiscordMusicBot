using System.Reflection;
using Discord.Commands;
using Discord.WebSocket;

namespace DiscordMusicBot;

public class CommandHandler
{
    public readonly DiscordSocketClient _client;
    private readonly CommandService _commands;
    private readonly IServiceProvider _services;
    private readonly string _prefix;

    public CommandHandler(DiscordSocketClient client, CommandService commands, IServiceProvider services
    )
    {
        _client = client;
        _commands = commands;
        _services = services;
        _prefix = "!";
    }

    public async Task InitializeAsync()
    {
        _client.MessageReceived += HandleCommandAsync;
        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
    }

    private async Task HandleCommandAsync(SocketMessage rawMessage)
    {
        if (!(rawMessage is SocketUserMessage message)) return;
        if (message.Source != Discord.MessageSource.User) return;

        int argPos = 0;
        if (!(message.HasStringPrefix(_prefix, ref argPos) ||
              message.HasMentionPrefix(_client.CurrentUser, ref argPos)))
            return;

        var context = new SocketCommandContext(_client, message);

        var result = await _commands.ExecuteAsync(context, argPos, _services);
        if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
        {
            await context.Channel.SendMessageAsync(result.ErrorReason);
        }
    }
}