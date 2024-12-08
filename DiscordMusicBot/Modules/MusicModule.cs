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
        await _musicService.PlayAsync(Context.Guild, query);
        await ReplyAsync("Now playing...");
    }

    [Command("stop", RunMode = RunMode.Async)]
    public async Task StopAsync()
    {
        await _musicService.StopAsync(Context.Guild);
        await ReplyAsync("Stopped playback.");
    }
}