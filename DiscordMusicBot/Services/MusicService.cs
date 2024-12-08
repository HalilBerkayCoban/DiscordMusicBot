using System.Diagnostics;
using Concentus.Structs;
using Concentus.Enums;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using DiscordMusicBot.Models;
using System.Collections.Concurrent;

namespace DiscordMusicBot.Services;

public class MusicService
{
    private readonly DiscordSocketClient _client;

    private readonly Dictionary<ulong, IAudioClient> _connectedChannels = new();
    private readonly Dictionary<ulong, CancellationTokenSource> _playbackCts = new();

    private readonly Dictionary<ulong, ConcurrentQueue<Song>> _songQueues = new();
    private readonly Dictionary<ulong, bool> _isPaused = new();

    public MusicService(DiscordSocketClient client)
    {
        _client = client;
    }

    public async Task JoinChannelAsync(IVoiceChannel channel)
    {
        try
        {
            if (channel == null)
                return;
            var audioClient = await channel.ConnectAsync();
            var cts = new CancellationTokenSource();
            _connectedChannels[channel.GuildId] = audioClient;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public async Task<string> PlayAsync(IGuild guild, string url)
    {
        if (!_connectedChannels.TryGetValue(guild.Id, out var audioClient))
            return "Bot is not connected to a voice channel.";

        if (!_songQueues.ContainsKey(guild.Id))
            _songQueues[guild.Id] = new ConcurrentQueue<Song>();

        var song = new Song { Url = url };
        _songQueues[guild.Id].Enqueue(song);

        if (_playbackCts.ContainsKey(guild.Id))
            return $"Added to queue: {url}";

        var cts = new CancellationTokenSource();
        _playbackCts[guild.Id] = cts;


        _ = Task.Run(async () =>
        {
            try
            {
                while (_songQueues[guild.Id].TryDequeue(out var nextSong))
                {
                    var cts = new CancellationTokenSource();
                    _playbackCts[guild.Id] = cts;

                    try
                    {
                        await InternalPlayAsync(audioClient, nextSong.Url, guild.Id, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("Song was skipped.");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error during playback: {e.Message}");
                    }
                    finally
                    {
                        cts.Dispose();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in playback loop: {e.Message}");
            }
            finally
            {
                _playbackCts.Remove(guild.Id);
            }
        }, cts.Token);

        return $"Now playing: {url}";
    }

    public Task StopAsync(IGuild guild)
    {
        if (_playbackCts.TryGetValue(guild.Id, out var cts))
        {
            cts.Cancel();
            _playbackCts.Remove(guild.Id);
        }

        return Task.CompletedTask;
    }

    private async Task InternalPlayAsync(IAudioClient audioClient, string url, ulong guildId, CancellationToken ct)
    {
        try
        {
            using var ytDlp = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = $"-f bestaudio -o - {url}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            ytDlp.Start();

            using var ffmpeg = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-hide_banner -i pipe:0 -f s16le -ar 48000 -ac 2 pipe:1",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            ffmpeg.Start();

            _ = Task.Run(async () =>
            {
                await ytDlp.StandardOutput.BaseStream.CopyToAsync(ffmpeg.StandardInput.BaseStream);
                ffmpeg.StandardInput.BaseStream.Close();
            }, ct);

            using var pcmStream = ffmpeg.StandardOutput.BaseStream;

            var opusEncoder = new OpusEncoder(48000, 2, OpusApplication.OPUS_APPLICATION_AUDIO);
            opusEncoder.Bitrate = 128000;

            int frameSize = 960;
            var buffer = new byte[frameSize * 2 * 2];
            var outBuffer = new byte[4000];

            using var discordBot = audioClient.CreateOpusStream();

            int bytesRead;
            while ((bytesRead = pcmStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (_isPaused.TryGetValue(guildId, out var isPaused) && isPaused)
                {
                    while (_isPaused[guildId])
                    {
                        await Task.Delay(100, ct);
                    }
                }

                short[] pcm = new short[frameSize * 2];
                Buffer.BlockCopy(buffer, 0, pcm, 0, buffer.Length);

                int encoded = opusEncoder.Encode(pcm, 0, frameSize, outBuffer, 0, outBuffer.Length);
                if (encoded > 0)
                {
                    await discordBot.WriteAsync(outBuffer.AsMemory(0, encoded), ct);
                }
            }
            await discordBot.FlushAsync(ct);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public Task<string> ShowQueueAsync(IGuild guild)
    {
        if (!_songQueues.TryGetValue(guild.Id, out var queue) || queue.IsEmpty)
        {
            return Task.FromResult("The queue is empty.");
        }

        var songList = string.Join("\n", queue.Select((s, i) => $"{i + 1}. {s.Title ?? s.Url}"));
        return Task.FromResult($"Current queue:\n{songList}");
    }

    public Task<string> PauseAsync(IGuild guild)
    {
        if (!_connectedChannels.ContainsKey(guild.Id))
            return Task.FromResult("The bot is not connected to a voice channel.");

        if (!_playbackCts.ContainsKey(guild.Id) || !_songQueues.ContainsKey(guild.Id))
            return Task.FromResult("Nothing is currently playing.");

        if (_isPaused.TryGetValue(guild.Id, out var isPaused) && isPaused)
            return Task.FromResult("Playback is already paused.");

        _isPaused[guild.Id] = true;

        return Task.FromResult("Playback paused.");
    }

    public Task<string> ResumeAsync(IGuild guild)
    {
        if (!_connectedChannels.ContainsKey(guild.Id))
            return Task.FromResult("The bot is not connected to a voice channel.");

        if (!_playbackCts.ContainsKey(guild.Id) || !_songQueues.ContainsKey(guild.Id))
            return Task.FromResult("Nothing is currently paused.");

        if (!_isPaused.TryGetValue(guild.Id, out var isPaused) || !isPaused)
            return Task.FromResult("Playback is not paused.");

        _isPaused[guild.Id] = false;

        return Task.FromResult("Playback resumed.");
    }

    public Task<string> SkipAsync(IGuild guild)
    {
        if (!_connectedChannels.ContainsKey(guild.Id))
            return Task.FromResult("The bot is not connected to a voice channel.");

        if (!_playbackCts.TryGetValue(guild.Id, out var cts))
            return Task.FromResult("No song is currently playing to skip.");

        cts.Cancel();
        return Task.FromResult("Skipped the current song.");
    }
}
