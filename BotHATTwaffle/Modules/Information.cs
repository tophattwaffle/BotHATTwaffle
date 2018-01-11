using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BotHATTwaffle.Modules
{
    public class InformationService
    {
        private DataServices _data;

        public InformationService(DataServices data)
        {
            _data = data;
        }
    }

    public class InformationModule : ModuleBase<SocketCommandContext>
    {
        private readonly DataServices _dataServices;

        public InformationModule(DataServices data)
        {
            _dataServices = data;
        }

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

            //If the URL isn't properly formatted, default
            if (!Uri.IsWellFormedUriString(builtUrl, UriKind.Absolute))
            {
                builtUrl = "https://developer.valvesoftware.com/wiki/Main_Page";
                searchTerm = "Valve Developer Community";
            }

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

        //RunMode Async so the bot does not hang making a web request. We lose exceptions, but that is fine.
        [Command("search", RunMode = RunMode.Async)]
        [Summary("`>search [series] [SearchTerm]` searches a tutorial series.")]
        [Remarks("`>search [series] [SearchTerm]` searches our tutorial database for a result." +
            "\nThere are a few series you can search from. You can use `>tutorial all [SearchTerm]` to search them all. `All` does not search the FAQ" +
            "\nExample:" +
            "\n `>search v2 displacements` or `>search f leak`" +
            "\n `1` `V2Series` `v2`" +
            "\n `2` `CSGOBootcamp` `bc` `csgobootcamp`" +
            "\n `3` `3dsmax` `3ds`" +
            "\n `4` `WrittenTutorials` `written`" +
            "\n `5` `LegacySeries` `v1` `lg`" +
            "\n `6` `HammerTroubleshooting` `ht` `misc`" +
            "\n `7` `FAQ` `f`" +
            "\n\n `>s [series] [dump/all]` Example: `>s v2 all` Will display ALL tutorials in that series. It can be slow to respond, so please wait!" +
            "\nReally big thanks to Mark for helping make the JSON searching work!")]
        [Alias("s")]
        public async Task SearchAsync(string series, [Remainder]string search)
        {
            var wait = await ReplyAsync($":eyes: Searching for **{search}** in **{series}**. This may take a moment! :eyes:");
            await _dataServices.ChannelLog($"{Context.User} ran a search",$"Series: {series}\nSearch Term: {search}");
            bool isPrivate = false;

            if (Context.IsPrivate)
                isPrivate = true;

            List<List<string>> results = _dataServices.Search(series, search, isPrivate);

            if (search.ToLower() == "dump" || search.ToLower() == "all")
            {
                //[0]title
                //[1]url
                //[2]tags
                List<string> reply = new List<string>();
                string text = null;
                foreach (var r in results)
                {
                    text += $"[{r[0]}]({r[1]})\nTags: {r[2]}\n\n";

                    if(text.Length > 1800)
                    {
                        reply.Add(text);
                        text = null;
                    }
                }

                if (text != null && text.Length != 0)
                    reply.Add(text);

                try //If we cannot send a DM to the user, let them know.
                {
                    foreach (var s in reply)
                    {
                        var builder = new EmbedBuilder()
                        {
                            Color = new Color(243, 128, 72),

                            Description = s
                        };
                        await Context.User.SendMessageAsync("",false,builder);
                    }
                }
                catch
                {
                    await ReplyAsync("```\"dump\" and \"all\" search terms can ONLY send replies in a DM. This is to prevent flooding chat." +
                        " You got this message because you do not accept DMs from non-friends.```");
                }
                if (!isPrivate)
                    await wait.DeleteAsync();

                return;
            }

            if (results.Count == 0)
            {
                List<string> singleResult = new List<string>
                {
                    "Try a different search term",
                    "http://tophattwaffle.com/faq",
                    "I could not locate anything for the search term you provided. Please try a different search term.",
                    null
                };
                results.Add(singleResult);
            }

            foreach (var r in results)
            {
                var builder = new EmbedBuilder();
                var authBuilder = new EmbedAuthorBuilder();
                authBuilder = new EmbedAuthorBuilder()
                {
                    Name = r[0],
                    IconUrl = "https://cdn.discordapp.com/icons/111951182947258368/0e82dec99052c22abfbe989ece074cf5.png"
                };

                builder = new EmbedBuilder()
                {
                    Author = authBuilder,

                    Title = $"Click Here",
                    Url = r[1],
                    ThumbnailUrl = r[3],
                    Color = new Color(243,128,72),

                    Description = r[2]
                };
                await ReplyAsync("",false,builder);
            }
            if (!isPrivate)
                await wait.DeleteAsync();
        }

        [Command("tutorials")]
        [Summary("`>tutorials [Optional series]` Displays links to tutorial series")]
        [Remarks("`>tutorials [Optional series]` Example: `>tutorials` `>tutorials v2`" +
            "\nDisplays information about all tutorial series, or the specific one you're looking for" +
            "\n\n`1` `V2Series` `v2`" +
            "\n`2` `CSGOBootcamp` `bc` `csgobootcamp`" +
            "\n`3` `3dsmax` `3ds`" +
            "\n`4` `WrittenTutorials` `written`" +
            "\n`5` `LegacySeries` `v1` `lg`" +
            "\n`6` `HammerTroubleshooting` `ht`")]
        [Alias("t")]
        public async Task TutorialsAsync(string searchSeries = "all")
        {
            string authTitle = null;
            string authImgUrl = "https://cdn.discordapp.com/icons/111951182947258368/0e82dec99052c22abfbe989ece074cf5.png";
            string footText = null;
            string footImgUrl = Program._client.CurrentUser.GetAvatarUrl();
            string bodyTitle = "Click Here!";
            string bodyUrl = null;
            string bodyThumbUrl = null;  //"https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png"
            string bodyImageUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/header.png";
            string bodyDescription = null;
            #region TutorialsIf
            //V2
            if (searchSeries.ToLower() == "v2series" || searchSeries.ToLower() == "v2" || searchSeries.ToLower() == "1")
            {
                authTitle = "Version 2 Tutorial Series";

                bodyUrl = "https://goo.gl/XoVXzd";

                bodyDescription = "The Version 2 Tutorial series was created with the knowledge that I gained from creating the " +
                    "Version 1(Now Legacy) series of tutorials.The goal is to help someone who hasn’t ever touched the tools " +
                    "get up and running in Source Engine level design. You can watch them in any order, " +
                    "but they have been designed to build upon each other.";
            }
            //Bootcamp
            else if (searchSeries.ToLower() == "csgobootcamp" || searchSeries.ToLower() == "bc" || searchSeries.ToLower() == "2")
            {
                authTitle = "CSGO Level Design Bootcamp";

                bodyUrl = "https://goo.gl/srFBxe";

                bodyDescription = "The CSGO Boot Camp series was created for ECS to air during their Twitch streams between matches." +
                    " It is created to help someone with no experience with the level design tools learn everything they need to" +
                    " create a competitive CSGO level. Most these tutorials apply to every Source Engine game," +
                    " but a handful are specific to CSGO.";
            }
            //3dsmax
            else if (searchSeries.ToLower() == "3dsmax" || searchSeries.ToLower() == "3ds" || searchSeries.ToLower() == "3")
            {
                authTitle = "3dsmax Tutorials";

                bodyUrl = "https://goo.gl/JGg48X";

                bodyDescription = "There are a few sub series in the 3dsmax section. If you’re looking to create and export your very first Source Engine prop, check out the **My First Prop** series." +
                    "\nIf you’re getting start with 3dsmax look at the **Beginners Guide** series, which is like the Version 2 Tutorial series but for 3dsmax." +
                    "\nThere are a few one - off tutorials listed on the page as well covering WallWorm functions";
            }
            //Writtentutorials
            else if (searchSeries.ToLower() == "writtentutorials" || searchSeries.ToLower() == "written" || searchSeries.ToLower() == "4")
            {
                authTitle = "Written Tutorials";

                bodyUrl = "https://goo.gl/i4aAqh";

                bodyDescription = "My library of written tutorials is typically about 1 off things that I want to cover. They are usually independent of any specific game.";
            }
            //legacy
            else if (searchSeries.ToLower() == "legacyseries" || searchSeries.ToLower() == "v1" || searchSeries.ToLower() == "lg" || searchSeries.ToLower() == "5")
            {
                authTitle = "Legacy Series";

                bodyUrl = "https://goo.gl/aHFcvX";

                bodyDescription = "Hammer Troubleshooting is a smaller series that is created off user questions that I see come up quite often.y are usually independent of any specific game.";
            }
            //Hammer Troubleshooting
            else if (searchSeries.ToLower() == "hammertroubleshooting" || searchSeries.ToLower() == "ht" || searchSeries.ToLower() == "6")
            {
                authTitle = "Hammer Troubleshooting";

                bodyUrl = "https://goo.gl/tBh7jT";

                bodyDescription = "The First tutorial series was my launching point for getting better at mapping. Not only did I learn a lot from making it, but I like to " +
                    "think that many others learned something from the series as well. The series was flawed in that it was not structured, and lacked quality control. But" +
                    " you may notice that the further along in the series you are, the better quality they get. Example is the 100th tutorial, it heavily reflects how the " +
                    "V2 series was created. You can view the entire series below. Just be warned that some of the information in these videos may not be correct, or even " +
                    "work any longer. Please watch at your own risk. I attempt to support these tutorials, but cannot due to time. Please watch the V2 series";
            }
            else if (searchSeries.ToLower() == "all")
            {
                authTitle = "All Tutorial Series Information";

                bodyUrl = "https://www.tophattwaffle.com/tutorials/";

                bodyDescription = $"Over the years I've built up quite the collection of tutorial series! " +
                    $"\n__Here they all are__" +
                    $"\n[Version 2 Series](https://goo.gl/XoVXzd)" +
                    $"\n[CSGO Bootcamp](https://goo.gl/srFBxe)" +
                    $"\n[3dsmax](https://goo.gl/JGg48X)" +
                    $"\n[Written Tutorials](https://goo.gl/i4aAqh)" +
                    $"\n[Hammer Troubleshooting](https://goo.gl/tBh7jT)" +
                    $"\n[Legacy Series V1](https://goo.gl/aHFcvX)";
            }
            else {
                await ReplyAsync("Unknown series. Please try `>help tutorials` to see all the options.");
                return; }
            #endregion
            var builder = new EmbedBuilder();
            var authBuilder = new EmbedAuthorBuilder();
            var footBuilder = new EmbedFooterBuilder();
            authBuilder = new EmbedAuthorBuilder()
            {
                Name = authTitle,
                IconUrl = authImgUrl
            };

            footBuilder = new EmbedFooterBuilder()
            {
                Text = footText,
                IconUrl = footImgUrl
            };

            builder = new EmbedBuilder()
            {
                Author = authBuilder,
                Footer = footBuilder,

                Title = bodyTitle,
                Url = bodyUrl,
                ImageUrl = bodyImageUrl,
                ThumbnailUrl = bodyThumbUrl,
                Color = new Color(243,128,72),

                Description = bodyDescription
            };
            await ReplyAsync("",false,builder);

        }


        [Command("catFact")]
        [Summary("`>catFact` Gives you a cat fact!")]
        [Remarks("Ever want to know more about cats? Now you can.")]
        [Alias("gimme a cat fact", "hit me with a cat fact", "hit a nigga with a cat fact", "cat fact", "cat fact", "catfacts", "cat facts")]
        public async Task CatFactAsync()
        {
            var catFact = "Did you know cats have big bushy tails?";
            var name = "Cat Fact 0";

            if (File.Exists(_dataServices.catFactPath))
            {
                var _rand = new Random();

                string[] allLines = File.ReadAllLines(_dataServices.catFactPath);
                int lineNumber = _rand.Next(0, allLines.Length);
                catFact = allLines[lineNumber];

                Match match = Regex.Match(catFact, @"^\w+ Fact \d*", RegexOptions.IgnoreCase);
                name = match.Value;
                catFact = catFact.Substring(match.Length).Trim();
            }

            var builder = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder {
                    Name = "CAT FACTS!",
                    IconUrl = Context.Message.Author.GetAvatarUrl(),
                },
                Footer = new EmbedFooterBuilder {
                    Text = "This was cat facts, you cannot unsubscribe."
                },

                ThumbnailUrl = "https://content.tophattwaffle.com/BotHATTwaffle/cat.png",
                Color = new Color(230, 235, 240),

                Fields =
                {
                    new EmbedFieldBuilder
                    {
                        Name = name,
                        Value = catFact
                    }
                }
            };

            await _dataServices.ChannelLog($"{Context.Message.Author.Username.ToUpper()} JUST GOT HIT WITH A CAT FACT");
            await ReplyAsync("", false, builder.Build());
        }

        [Command("unsubscribe")]
        [Summary("`>unsubscribe` Unsubscribes you from cat facts")]
        [Remarks("Takes you off the cat fact list.")]
        public async Task CatFactUnsubAsync()
        {
            await ReplyAsync("You cannot unsubscribe from cat facts...");
        }

        [Command("PenguinFact")]
        [Summary("`>PenguinFact` Gives you a Penguin fact!")]
        [Remarks("Ever want to know more about Penguin? Now you can.")]
        [Alias("gimme a penguin fact", "hit me with a penguin fact", "hit a nigga with a penguin fact", "penguin fact", "penguin fact", "penguinfacts", "penguin facts")]
        public async Task PenguinFactAsync()
        {
            Random _rand = new Random();

            string penguinFact = "Did you know penguins have big bushy tails?";
            if (File.Exists(_dataServices.penguinFactPath))
            {
                var allLines = File.ReadAllLines(_dataServices.penguinFactPath);
                var lineNumber = _rand.Next(0, allLines.Length);
                penguinFact = allLines[lineNumber];
            }

            var authBuilder = new EmbedAuthorBuilder()
            {
                Name = $"PENGUIN FACTS!",
                IconUrl = Context.Message.Author.GetAvatarUrl(),
            };

            var footBuilder = new EmbedFooterBuilder()
            {
                Text = "This was penguin facts, you cannot unsubscribe."
            };
            var builder = new EmbedBuilder()
            {
                Author = authBuilder,
                Footer = footBuilder,

                ThumbnailUrl = "https://content.tophattwaffle.com/BotHATTwaffle/penguin.png",
                Color = new Color(230, 235, 240),

                Description = penguinFact
            };
            await _dataServices.ChannelLog($"{Context.Message.Author.Username.ToUpper()} JUST GOT HIT WITH A PENGUIN FACT");
            await ReplyAsync("", false, builder.Build());
        }
		
		[Command("TanookiFact")]
        [Summary("`>tanookiFact` Gives you a Tanooki fact!")]
        [Remarks("Ever want to know more about Tanooki? Now you can.")]
        [Alias("gimme a tanooki fact", "hit me with a tanooki fact", "hit a nigga with a tanooki fact", "tanooki fact", "tanookifacts", "tanooki facts", "@TanookiSuit3")]
        public async Task TanookiFactAsync()
        {
            Random _rand = new Random();

            string tanookiFact = "Did you know Tanooki has a big bushy tail?";
            if (File.Exists(_dataServices.tanookiFactPath))
            {
                var allLines = File.ReadAllLines(_dataServices.tanookiFactPath);
                var lineNumber = _rand.Next(0, allLines.Length);
                tanookiFact = allLines[lineNumber];
            }

            var authBuilder = new EmbedAuthorBuilder()
            {
                Name = $"TANOOKI FACTS!",
                IconUrl = Context.Message.Author.GetAvatarUrl(),
            };

            var footBuilder = new EmbedFooterBuilder()
            {
                Text = "This was Tanooki facts, you cannot unsubscribe."
            };
            var builder = new EmbedBuilder()
            {
                Author = authBuilder,
                Footer = footBuilder,

                ThumbnailUrl = "https://content.tophattwaffle.com/BotHATTwaffle/tanookisuit.png",
                Color = new Color(230, 235, 240),

                Description = tanookiFact
            };
            await _dataServices.ChannelLog($"{Context.Message.Author.Username.ToUpper()} JUST GOT HIT WITH A TANOOKI FACT");
            await ReplyAsync("", false, builder.Build());
        }
    }
}
