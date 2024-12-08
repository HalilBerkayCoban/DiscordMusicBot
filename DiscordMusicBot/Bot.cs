using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordMusicBot.Services;
using DotNetEnv;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordMusicBot;

public class Bot
{
    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _services;

    public Bot()
    {
        var clientConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent |
                             GatewayIntents.GuildVoiceStates
        };
        _client = new DiscordSocketClient(clientConfig);

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        _services = serviceCollection.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(_client).AddSingleton(new CommandService()).AddSingleton<CommandHandler>()
            .AddScoped<MusicService>();
    }

    public async Task RunAsync()
    {
        var token = Environment.GetEnvironmentVariable("BOT_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
        {
            Console.WriteLine("Bot token is missing.");
            return;
        }

        _client.Log += (msg) =>
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        };

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        var cmdHandler = _services.GetRequiredService<CommandHandler>();
        await cmdHandler.InitializeAsync();

        await Task.Delay(-1);
    }
}