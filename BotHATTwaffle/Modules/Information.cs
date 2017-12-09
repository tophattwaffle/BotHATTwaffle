using Discord;
using Discord.Commands;
using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace BotHATTwaffle.Modules
{
    public class Information : ModuleBase<SocketCommandContext>
    {
        [Command("vdc")]
        [Summary("`>vdc [Search]` Quick link back to a VDC search")]
        [Remarks("Does a search on the VDC and gives you the link back. Try to use the proper full term, for instance: " +
            "`func_detail` will give you better results than `detail`")]
        [Alias("v")]
        public async Task SearchAsync([Remainder] string searchTerm)
        {
            await Context.Channel.TriggerTypingAsync();

            searchTerm = searchTerm.Replace(' ','+');
            string builtUrl = $"https://developer.valvesoftware.com/w/index.php?search={searchTerm}&title=Special%3ASearch&go=Go";

            //Download webpage title and store to string
            WebClient x = new WebClient();
            string siteTitle = x.DownloadString(builtUrl);
            string regex = @"(?<=<title.*>)([\s\S]*)(?=</title>)";
            Regex ex = new Regex(regex, RegexOptions.IgnoreCase);
            siteTitle = ex.Match(siteTitle).Value.Trim();

            var builder = new EmbedBuilder();
            var authBuilder = new EmbedAuthorBuilder();
            var footBuilder = new EmbedFooterBuilder();
            authBuilder = new EmbedAuthorBuilder()
            {
                Name = $"This is what I was able to find for {searchTerm}",
                IconUrl = "https://cdn.discordapp.com/icons/111951182947258368/0e82dec99052c22abfbe989ece074cf5.png"
            };

            footBuilder = new EmbedFooterBuilder()
            {
                Text = "Thanks for using the VDC search!",
                IconUrl = Program._client.CurrentUser.GetAvatarUrl()
            };

            builder = new EmbedBuilder()
            {
                Author = authBuilder,
                Footer = footBuilder,

                Title = $"**Search Results**",
                Url = builtUrl,
                ImageUrl = "https://developer.valvesoftware.com/w/skins/valve/images-valve/logo.png",
                //ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
                Color = new Color(71, 126, 159),

                Description = siteTitle
            };

            await ReplyAsync("",false,builder);
        }

        [Command("catFact")]
        [Summary("`>catFact` Gives you a cat fact!")]
        [Remarks("Ever want to know more about cats? Now you can.")]
        [Alias("gimme a cat fact", "hit me with a cat fact", "hit a nigga with a cat fact", "cat fact")]
        public async Task CatFactAsync()
        {
            Random _rand = new Random();
            string path = null;
            if (Program.config.ContainsKey("catFactPath"))
                path = (Program.config["catFactPath"]);

            string catFact = "Did you know cats have big bushy tails?";
            if (File.Exists(path))
            {
                var allLines = File.ReadAllLines(path);
                var lineNumber = _rand.Next(0, allLines.Length);
                catFact = allLines[lineNumber];
            }

            var authBuilder = new EmbedAuthorBuilder()
            {
                Name = $"CAT FACTS!",
                IconUrl = Context.Message.Author.GetAvatarUrl(),
            };

            var footBuilder = new EmbedFooterBuilder()
            {
                Text = "This was cat facts, you cannot unsubscribe."
            };

            var builder = new EmbedBuilder()
            {
                Author = authBuilder,
                Footer = footBuilder,

                ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
                Color = new Color(230, 235, 240),

                Description = catFact
            };
            await Program.ChannelLog($"{Context.Message.Author.Username.ToUpper()} JUST GOT HIT WITH A CAT FACT");
            await ReplyAsync("", false, builder.Build());
        }

        [Command("unsubscribe")]
        [Summary("`>unsubscribe` Unsubscribes your from cat facts")]
        [Remarks("Takes you off the cat fact list.")]
        public async Task CatFactUnsubAsync()
        {
            await ReplyAsync("You cannot unsubscribe from cat facts...");
        }
    }
}
