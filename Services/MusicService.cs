using System;
using Discord;
using Discord.WebSocket;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using StreamMusicBot.Modules;
using Victoria;
using Victoria.Entities;

namespace StreamMusicBot.Services
{
    public class MusicService
    {
        private readonly LavaRestClient _lavaRestClient;
        private readonly LavaSocketClient _lavaSocketClient;
        private readonly DiscordSocketClient _client;
        private readonly LogService _logService;

        public MusicService(LavaRestClient lavaRestClient, DiscordSocketClient client,
            LavaSocketClient lavaSocketClient, LogService logService)
        {
            _client = client;
            _lavaRestClient = lavaRestClient;
            _lavaSocketClient = lavaSocketClient;
            _logService = logService;
        }

        public Task InitializeAsync()
        {
            _client.Ready += ClientReadyAsync;
            _lavaSocketClient.Log += LogAsync;
            _lavaSocketClient.OnTrackFinished += TrackFinished;
            return Task.CompletedTask;
        }

        public async Task ConnectAsync(SocketVoiceChannel voiceChannel, ITextChannel textChannel)
            => await _lavaSocketClient.ConnectAsync(voiceChannel, textChannel);

        public async Task LeaveAsync(SocketVoiceChannel voiceChannel)
            => await _lavaSocketClient.DisconnectAsync(voiceChannel);

        public async Task Loop(ulong guildId, ICommandContext context)
        {
            var _player = _lavaSocketClient.GetPlayer(guildId);
            while (true)
            {
                TimeSpan len = _player.CurrentTrack.Length;
                double length = len.TotalSeconds;
                int count = 0;
                while (count < length)
                {
                    count++;
                    Thread.Sleep(1000);
                }

                if (_player.Queue.Items.Count() is 0)
                    continue;
                await _player.SkipAsync();
                await context.Channel.SendMessageAsync($"Now Playing, {_player.CurrentTrack.Title}");
            }
        }

        public async Task<string> PlayAsync(string query, ulong guildId)
        {
            var _player = _lavaSocketClient.GetPlayer(guildId);
            var results = await _lavaRestClient.SearchYouTubeAsync(query);
            if (results.LoadType == LoadType.NoMatches || results.LoadType == LoadType.LoadFailed)
            {
                return "No Video found";
            }

            var track = results.Tracks.FirstOrDefault();

            if (_player.IsPlaying)
            {
                _player.Queue.Enqueue(track);
                return $"{track.Title} was found, added to queue";
            }
            else
            {
                await _player.PlayAsync(track);
                return $"Now Playing, {track.Title}!";
            }
        }

        public async Task<string> StopAsync(ulong guildId)
        {
            var _player = _lavaSocketClient.GetPlayer(guildId);
            if (_player is null)
                return "Oops Player died lol";
            await _player.StopAsync();
            return "Stopped?!";
        }

        public async Task<string> SkipAsync(ulong guildId)
        {
            var _player = _lavaSocketClient.GetPlayer(guildId);
            if (_player is null || _player.Queue.Items.Count() is 0)
                return "Nothin in the queue my g";

            var oldTrack = _player.CurrentTrack;
            await _player.SkipAsync();
            return $"Skipped the song, {oldTrack.Title} \nNow Playing, {_player.CurrentTrack.Title}";
        }

        public async Task<string> SetVolumeAsync(int vol, ulong guildId)
        {
            var _player = _lavaSocketClient.GetPlayer(guildId);
            if (_player is null)
                return "Nothin is playin";

            if (vol > 150 || vol <= 2)
            {
                return "Invalid Volume u wigga";
            }

            await _player.SetVolumeAsync(vol);
            return $"Volume set to, {vol}";
        }

        public async Task<string> PauseOrResumeAsync(ulong guildId)
        {
            var _player = _lavaSocketClient.GetPlayer(guildId);
            if (_player is null)
                return "Nothin playin";

            if (!_player.IsPaused)
            {
                await _player.PauseAsync();
                return "Paused that shit";
            }
            else
            {
                await _player.ResumeAsync();
                return "playin that shit";
            }
        }

        public async Task<string> ResumeAsync(ulong guildId)
        {
            var _player = _lavaSocketClient.GetPlayer(guildId);
            if (_player is null)
                return "Nothin playin";

            if (_player.IsPaused)
            {
                await _player.ResumeAsync();
                return "playin that shit";
            }

            return "Shits already playin";
        }


        private async Task ClientReadyAsync()
        {
            await _lavaSocketClient.StartAsync(_client);
        }

        private async Task TrackFinished(LavaPlayer player, LavaTrack track, TrackEndReason reason)
        {
            if (!reason.ShouldPlayNext())
                return;

            if (!player.Queue.TryDequeue(out var item) || !(item is LavaTrack nextTrack))
            {
                await player.TextChannel.SendMessageAsync("There are no more tracks in the queue.");
                return;
            }

            await player.PlayAsync(nextTrack);
        }

        private async Task LogAsync(LogMessage logMessage)
        {
            await _logService.LogAsync(logMessage);
        }
    }
}
