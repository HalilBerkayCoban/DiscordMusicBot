using DiscordMusicBot;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostContext, config) => { config.AddEnvironmentVariables(); })
            .ConfigureServices((hostContext, services) => { });

        Env.TraversePath().Load();
        var host = builder.Build();
        var bot = new Bot();
        await bot.RunAsync();
        await host.RunAsync();
    }
}