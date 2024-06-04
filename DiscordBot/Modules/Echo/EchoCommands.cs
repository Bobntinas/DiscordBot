using Discord.Commands;

namespace DiscordBot.Modules.Echo
{
    public class EchoCommands : ModuleBase<SocketCommandContext>
    {
        [Command("echo")]
        [Summary("Echoes back what was said")]

        public async Task ExecuteAsync([Remainder][Summary("The text to echo")] string echo)
        => await ReplyAsync(echo);
    }
}
