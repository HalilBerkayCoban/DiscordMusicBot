using Discord.Commands;
using Discord.WebSocket;
using DiscordMusicBot.Services;

namespace DiscordMusicBot.Modules;

public class MusicModule : ModuleBase<SocketCommandContext>
{
    private readonly MusicService _musicService;

    public MusicModule(MusicService musicService)
    {
        _musicService = musicService;
    }

    [Command("join", RunMode = RunMode.Async)]
    public async Task JoinAsync()
    {
        var voiceChannel = (Context.User as SocketGuildUser)?.VoiceChannel;
        if (voiceChannel == null)
        {
            await ReplyAsync("You need to join a voice channel first.");
            return;
        }

        await _musicService.JoinChannelAsync(voiceChannel);
        await ReplyAsync($"Joined {voiceChannel.Name}");
    }

    [Command("play", RunMode = RunMode.Async)]
    public async Task PlayAsync([Remainder] string query)
    {
        var guild = Context.Guild;
        if (guild == null)
        {
            await ReplyAsync("This command can only be used in a server.");
            return;
        }

        var result = await _musicService.PlayAsync(guild, query);
        await ReplyAsync(result);
    }

    [Command("stop", RunMode = RunMode.Async)]
    public async Task StopAsync()
    {
        await _musicService.StopAsync(Context.Guild);
        await ReplyAsync("Stopped playback.");
    }

    [Command("queue", RunMode = RunMode.Async)]
    public async Task ShowQueueAsync()
    {
        var guild = Context.Guild;
        if (guild == null)
        {
            await ReplyAsync("This command can only be used in a server.");
            return;
        }

        var result = await _musicService.ShowQueueAsync(guild);
        await ReplyAsync(result);
    }

    [Command("pause", RunMode = RunMode.Async)]
    public async Task PauseAsync()
    {
        var guild = Context.Guild;
        if (guild == null)
        {
            await ReplyAsync("This command can only be used in a server.");
            return;
        }

        var result = await _musicService.PauseAsync(guild);
        await ReplyAsync(result);
    }

    [Command("resume", RunMode = RunMode.Async)]
    public async Task ResumeAsync()
    {
        var guild = Context.Guild;
        if (guild == null)
        {
            await ReplyAsync("This command can only be used in a server.");
            return;
        }

        var result = await _musicService.ResumeAsync(guild);
        await ReplyAsync(result);
    }
}