using System.Diagnostics;
using Concentus.Structs;
using Concentus.Enums;
using Discord;
using Discord.Audio;
using Discord.WebSocket;

namespace DiscordMusicBot.Services;

public class MusicService
{
    private readonly DiscordSocketClient _client;

    private readonly Dictionary<ulong, IAudioClient> _connectedChannels = new();
    private readonly Dictionary<ulong, CancellationTokenSource> _playbackCts = new();

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
            _ = KeepAliveSilenceAsync(audioClient, cts.Token, TimeSpan.FromHours(1));
            _connectedChannels[channel.GuildId] = audioClient;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public async Task PlayAsync(IGuild guild, string url)
    {
        if (!_connectedChannels.TryGetValue(guild.Id, out var audioClient))
            return;

        if (_playbackCts.TryGetValue(guild.Id, out var oldCts))
        {
            oldCts.Cancel();
            _playbackCts.Remove(guild.Id);
        }

        var cts = new CancellationTokenSource();
        _playbackCts[guild.Id] = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await InternalPlayAsync(audioClient, url, cts.Token);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }, cts.Token);
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

    private async Task InternalPlayAsync(IAudioClient audioClient, string url, CancellationToken ct)
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
            Console.WriteLine("Starting yt-dlp");
            ytDlp.Start();
            Console.WriteLine("Quiting yt-dlp");

            using var ffmpeg = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1",
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
                if (bytesRead < buffer.Length)
                    break;

                short[] pcm = new short[frameSize * 2];
                Buffer.BlockCopy(buffer, 0, pcm, 0, buffer.Length);

                int encoded = opusEncoder.Encode(pcm, 0, frameSize, outBuffer, 0, outBuffer.Length);
                if (encoded > 0)
                {
                    await discordBot.WriteAsync(outBuffer.AsMemory(0, encoded), ct);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private async Task KeepAliveSilenceAsync(IAudioClient audioClient, CancellationToken ct, TimeSpan duration)
    {
        var opusEncoder = new OpusEncoder(48000, 2, Concentus.Enums.OpusApplication.OPUS_APPLICATION_AUDIO)
        {
            Bitrate = 128000
        };

        short[] pcmSilence = new short[960 * 2]; // 0-filled by default, represents silence.
        byte[] encodedBuffer = new byte[4000];

        using var discordOut = audioClient.CreateOpusStream();
        var endTime = DateTime.UtcNow + duration;

        while (DateTime.UtcNow < endTime && !ct.IsCancellationRequested)
        {
            int encoded = opusEncoder.Encode(pcmSilence, 0, 960, encodedBuffer, 0, encodedBuffer.Length);
            if (encoded > 0)
            {
                await discordOut.WriteAsync(encodedBuffer.AsMemory(0, encoded), ct);
            }

            await Task.Delay(200, ct);
        }

        await discordOut.FlushAsync();
    }
}