using System.Threading;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using StreamMusicBot.Services;
using System.Threading.Tasks;
namespace StreamMusicBot.Modules
{
    public class Music : ModuleBase<SocketCommandContext>
    {
        private MusicService _musicService;
        private bool isRun = false;

        public Music(MusicService musicService)
        {
            _musicService = musicService;
        }

        [Command("Help")]
        public async Task Help()
        {
            var user = Context.User as SocketGuildUser;
            await Reply($"Commands =>\n" +
                        $"Join = " +
                        $""
            );
        }

        [Command("Join")]
        public async Task Join()
        {
            var user = Context.User as SocketGuildUser;
            if (user.VoiceChannel is null)
            {
                await Reply("You need to connect to a voice channel.");
                return;
            }
            else
            {
                await _musicService.ConnectAsync(user.VoiceChannel, Context.Channel as ITextChannel);
                await Reply($"now connected to {user.VoiceChannel.Name}");
            }
        }

        public async void RunLoop(ulong g, ICommandContext c)
        {
            await _musicService.Loop(g, c);
        }

        [Command("Leave")]
        public async Task Leave()
        {
            var user = Context.User as SocketGuildUser;
            if (user.VoiceChannel is null)
            {
                await Reply("Please join the channel the bot is in to make it leave.");
            }
            else
            {
                await _musicService.LeaveAsync(user.VoiceChannel);
                await Reply($"Bot has now left {user.VoiceChannel.Name}");
            }
        }

        [Command("Play")]
        public async Task Play([Remainder] string query)
        {
            await Reply(await _musicService.PlayAsync(query, Context.Guild.Id));
        }

        [Command("Stop")]
        public async Task Stop()
            => await Reply(await _musicService.StopAsync(Context.Guild.Id));

        [Command("Skip")]
        public async Task Skip()
            => await Reply(await _musicService.SkipAsync(Context.Guild.Id));

        [Command("Volume")]
        public async Task Volume(int vol)
            => await Reply(await _musicService.SetVolumeAsync(vol, Context.Guild.Id));

        [Command("Pause")]
        public async Task Pause()
            => await Reply(await _musicService.PauseOrResumeAsync(Context.Guild.Id));

        [Command("Resume")]
        public async Task Resume()
            => await Reply(await _musicService.ResumeAsync(Context.Guild.Id));
        
        protected virtual async Task<IUserMessage> Reply(
            string message = null,
            bool isTTS = false,
            Embed embed = null,
            RequestOptions options = null)
        {
            message = Context.User.Mention + "```\n"+message+"```";
            return await Context.Channel.SendMessageAsync(message, isTTS, embed, options).ConfigureAwait(false);
        }
    }
}
