using Discord;
using Discord.Commands;
//using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace BotHATTwaffle.Modules
{
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _service;
        //private readonly IConfigurationRoot _config;

        public HelpModule(CommandService service)
        {
            _service = service;
            //_config = config;
        }

        [Command("help")]
        [Summary("`>help` Displays this message")]
        [Alias("h")]
        public async Task HelpAsync()
        {
            if(!Context.IsPrivate)
                await Context.Message.DeleteAsync();
            //string prefix = _config["prefix"];
            var builder = new EmbedBuilder()
            {
                Color = new Color(47,111,146),
                Description = "These are the commands you can use"
            };

            foreach (var module in _service.Modules)
            {
                string description = null;
                foreach (var cmd in module.Commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (result.IsSuccess)
                        description += $"{cmd.Aliases.First()} - {cmd.Summary}\nAlias: {string.Join(", ", cmd.Aliases.ToArray())}\n";
                }

                if (!string.IsNullOrWhiteSpace(description))
                {
                    builder.AddField(x =>
                    {
                        x.Name = module.Name;
                        x.Value = description;
                        x.IsInline = false;
                    });
                }
            }

            await Context.User.SendMessageAsync("", false, builder.Build());
        }

        [Command("help")]
        [Summary("`>help [command]` Displays help message for a specific command")]
        [Alias("h")]
        public async Task HelpAsync(string command)
        {
            if (!Context.IsPrivate)
                await Context.Message.DeleteAsync();
            var result = _service.Search(Context, command);
            if (!result.IsSuccess)
            {
                await ReplyAsync($"Sorry, I couldn't find a command like **{command}**.");
                return;
            }

            //string prefix = _config["prefix"];
            var builder = new EmbedBuilder()
            {
                Color = new Color(47, 111, 146),
                Description = $"Here are some commands like **{command}**"
            };

            foreach (var match in result.Commands)
            {
                var cmd = match.Command;
                builder.AddField(x =>
                {
                    x.Name = string.Join(", ", cmd.Aliases);
                    x.Value = $"Parameters: {string.Join(", ", cmd.Parameters.Select(p => p.Name))}" +
                              $"\nSummary: {cmd.Summary}" +
                              $"\nInstructions: {cmd.Remarks}" +
                              $"\nAlias: {string.Join(", ", cmd.Aliases.ToArray())}"; ;
                    x.IsInline = false;
                });
            }

            await Context.User.SendMessageAsync("", false, builder.Build());
        }
    }
}