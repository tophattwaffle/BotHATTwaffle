#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
using Discord.WebSocket;
using System.Threading.Tasks;
using Discord;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace BotHATTwaffle
{
    class Eavesdropping
    {
        private readonly Timer _timer;
        int joinDelayRoleTime = 10;
        public List<UserData> joinDelayList = new List<UserData>();
        string[] pakRatEavesDrop;
        string[] howToPackEavesDrop;
        string[] carveEavesDrop;
        string[] propperEavesDrop;
        string[] vbEavesDrop;
        string[] yorkEavesDrop;
        string[] agreeEavesDrop;
        string[] agreeStrings;
        Random _random;

        public Eavesdropping()
        {
            if (Program.config.ContainsKey("pakRatEavesDropCSV"))
                pakRatEavesDrop = (Program.config["pakRatEavesDropCSV"]).Split(',') ;
            if (Program.config.ContainsKey("howToPackEavesDropCSV"))
                howToPackEavesDrop = (Program.config["howToPackEavesDropCSV"]).Split(',');
            if (Program.config.ContainsKey("carveEavesDropCSV"))
                carveEavesDrop = (Program.config["carveEavesDropCSV"]).Split(',');
            if (Program.config.ContainsKey("propperEavesDropCSV"))
                propperEavesDrop = (Program.config["propperEavesDropCSV"]).Split(',');
            if (Program.config.ContainsKey("vbEavesDropCSV"))
                vbEavesDrop = (Program.config["vbEavesDropCSV"]).Split(',');
            if (Program.config.ContainsKey("yorkCSV"))
                yorkEavesDrop = (Program.config["yorkCSV"]).Split(',');
            if (Program.config.ContainsKey("agreeUserCSV"))
                agreeEavesDrop = (Program.config["agreeUserCSV"]).Split(',');

            _random = new Random();

            agreeStrings = new string[]{
                "^",
                "^^^",
                "^^^ I agree with ^^^",
            };

            _timer = new Timer(_ =>
            {
                foreach (UserData u in joinDelayList.ToList())
                {
                    if (u.CanRole())
                    {
                        u.user.AddRoleAsync(Program.playTesterRole);
                        u.user.SendMessageAsync("", false, u.joinMessage);
                        joinDelayList.Remove(u);
                        Program.ChannelLog($"{u.user.Username} now has playtester role. Welcome DM was sent.");
                        Task.Delay(1000);
                    }
                }
            },
            null,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(60));
        }

        public void AddNewUserJoin(SocketGuildUser inUser, DateTime inRoleTime, Embed message)
        {
            Program.ChannelLog($"USER JOINED {inUser.Username}#{inUser.Discriminator}", $"I will apply a roles at {inRoleTime}. They will then have playtester and can talk." +
                $"\nCreated At: {inUser.CreatedAt}" +
                $"\nJoined At: {inUser.JoinedAt}" +
                $"\nUser ID: {inUser.Id}");
            joinDelayList.Add(new UserData()
            {
                user = inUser,
                joinRoleTime = inRoleTime,
                joinMessage = message,
            });
        }


        async internal Task UserJoin(SocketUser user)
        {
            var builder = new EmbedBuilder();
            var authBuilder = new EmbedAuthorBuilder();
            var footBuilder = new EmbedFooterBuilder();
            authBuilder = new EmbedAuthorBuilder()
            {
                Name = $"Hey there {user.Username}! Welcome to the r/sourceengine discord server!",
                IconUrl = "https://cdn.discordapp.com/icons/111951182947258368/0e82dec99052c22abfbe989ece074cf5.png"
            };

            footBuilder = new EmbedFooterBuilder()
            {
                Text = "Once again thanks for joining, hope you enjoy your stay!",
                IconUrl = Program._client.CurrentUser.GetAvatarUrl()
            };

            builder = new EmbedBuilder()
            {
                Author = authBuilder,
                Footer = footBuilder,

                Title = $"Thanks for joining!",
                Url = "https://www.tophattwaffle.com",
                ImageUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/12/Discord-LogoWordmark-Color.png",
                ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
                Color = new Color(243, 128, 72),

                Description = $"Hi there! Thanks for joining the SourceEngine Discord server!\n" +
                $"Now that the {joinDelayRoleTime} minute verification has ended, there are a few things I wanted to tell you! Feel free to ask a question in " +
                $"any of the relevant channels you see. Just try to keep things on topic. \n\nAdditionally, you've been given a role called" +
                $" `Playtester`. This role is used to notify you when we have a playtest starting. You can remove yourself from the " +
                $"notifications by typing: `>playtester`.\n\nIf you want to see any of my commands, type: `>help`. Thanks for reading," +
                $" and we hope you enjoy your stay here!" +
                $"\n\nThere are roles you can use to show what skills you have. To see what roles you can give yourself, type: `>roleme display`" +
                $"\n\nGLHF"
            };

            DateTime roleTime = DateTime.Now.AddMinutes(joinDelayRoleTime);
            builder.Build();
            AddNewUserJoin((SocketGuildUser)user, roleTime, builder.Build());
        }

        async internal Task Listen(SocketMessage message)
        {
            bool proceed = true;
            //If the message is from a bot, just break.
            if (message.Author.IsBot)
                return;

            //Let's check against the values defined in the settings file.

            if (proceed)
                if (message.Content.Contains(":KMS:") || message.Content.Contains(":ShootMyself:") || message.Content.Contains(":HangMe:"))
                {
                    var builder = new EmbedBuilder()
                    {
                        ImageUrl = "https://content.tophattwaffle.com/BotHATTwaffle/doit.jpg",
                    };
                    await message.Channel.SendMessageAsync("",false, builder);
                    proceed = false;
                }
            if (proceed)
                if (message.Content.ToLower().Equals("who is daddy") || message.Content.ToLower().Equals("who is tophattwaffle"))
                {
                    await message.Channel.SendMessageAsync("TopHATTwaffle my daddy.");
                    proceed = false;
                }
            if (proceed)
                foreach (string s in agreeEavesDrop)
                {
                    if (message.Content.Equals("^") && message.Author.Username.Equals(s))
                    {
                            await message.Channel.SendMessageAsync(agreeStrings[_random.Next(0, agreeStrings.Length)]);
                            proceed = false;
                            break;
                    }
                }
            if (proceed)
                foreach (string s in pakRatEavesDrop)
                {
                    if(message.Content.ToLower().Contains(s))
                    {
                        await PakRat(message);
                            proceed = false;
                        break;
                    }
                }
            if(proceed)
                foreach (string s in howToPackEavesDrop)
                {
                    if (message.Content.ToLower().Contains(s))
                    {
                        await HowToPack(message);
                        proceed = false;
                        break;
                    }
                }
            if (proceed)
                foreach (string s in carveEavesDrop)
                {
                    if (message.Content.ToLower().Contains(s))
                    {
                        await Carve(message);
                        proceed = false;
                        break;
                    }
                }
            if (proceed)
                foreach (string s in propperEavesDrop)
                {
                    if (message.Content.ToLower().Contains(s))
                    {
                        await Propper(message);
                        proceed = false;
                        break;
                    }
                }
            if (proceed)
                foreach (string s in vbEavesDrop)
                {
                    if (message.Content.ToLower().Contains(s))
                    {
                        await VB(message);
                        proceed = false;
                        break;
                    }
                }
            if (proceed)
                foreach (string s in yorkEavesDrop)
                {
                    if (message.Content.ToLower().Contains(s))
                    {
                        await DeYork(message);
                        proceed = false;
                        break;
                    }
                }
        }

        private static Task PakRat(SocketMessage message)
        {
            Program.ChannelLog($"{message.Author} was asking about PakRat in #{message.Channel}");

            var authBuilder = new EmbedAuthorBuilder() {
                Name = $"Hey there {message.Author.Username}!",
                IconUrl = message.Author.GetAvatarUrl(),
            };

            var builder = new EmbedBuilder() {
                Author = authBuilder,

                Title = $"Click here to learn how to use VIDE!",
                Url = "https://www.tophattwaffle.com/packing-custom-content-using-vide-in-steampipe/",
                ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2013/11/vide.png",
                Color = new Color(243, 128, 72),

                Description = "I was minding my own business when I heard you mention something about PakRat. " +
                "Don't know if you know this, but PakRat is super old and has been know to cause issues in newer games. " +
                "There is a newer program that handles packing better called VIDE. You should check that out instead."
            };

            message.Channel.SendMessageAsync("",false,builder);

            return Task.CompletedTask;
        }

        private static Task HowToPack(SocketMessage message)
        {
            Program.ChannelLog($"{message.Author} was asking how to pack a level in #{message.Channel}");

            var authBuilder = new EmbedAuthorBuilder()
            {
                Name = $"Hey there {message.Author.Username}!",
                IconUrl = message.Author.GetAvatarUrl(),
            };

            var builder = new EmbedBuilder()
            {
                Author = authBuilder,

                Title = $"Click here to learn how to use VIDE!",
                Url = "https://www.tophattwaffle.com/packing-custom-content-using-vide-in-steampipe/",
                ThumbnailUrl = "https://content.tophattwaffle.com/BotHATTwaffle/vide.png",
                Color = new Color(243, 128, 72),

                Description = $"I noticed you may be looking for information on how to pack custom content into your level. " +
                $"This is easily done using VIDE. Click the link above to download VIDE and learn how to use it."
            };

            message.Channel.SendMessageAsync("", false, builder);

            return Task.CompletedTask;
        }

        private static Task Carve(SocketMessage message)
        {
            Program.ChannelLog($"{message.Author} was asking how to carve in #{message.Channel}. You should probably kill them.");

            var authBuilder = new EmbedAuthorBuilder()
            {
                Name = $"Hey there {message.Author.Username}!",
                IconUrl = message.Author.GetAvatarUrl(),
            };

            var builder = new EmbedBuilder()
            {
                Author = authBuilder,

                Title = $"DO NOT USE CARVE",
                ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
                Color = new Color(243, 128, 72),

                Description = $"I was minding my own damn business when you come around asking how to carve." +
                $"\n\n\n**__DON'T__**"
            };

            message.Channel.SendMessageAsync("", false, builder);

            return Task.CompletedTask;
        }

        private static Task Propper(SocketMessage message)
        {
            Program.ChannelLog($"{message.Author} was asking about Propper in #{message.Channel}. You should go WWMT fanboy.");

            var authBuilder = new EmbedAuthorBuilder()
            {
                Name = $"Hey there {message.Author.Username}!",
                IconUrl = message.Author.GetAvatarUrl(),
            };

            var builder = new EmbedBuilder()
            {
                Author = authBuilder,

                Title = $"Click here to go to the WallWorm site!",
                Url = "https://dev.wallworm.com/",
                ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/12/worm_logo.png",
                Color = new Color(243, 128, 72),

                Description = $"I saw you were asking about propper. While Propper still works, it's advised to learn" +
                $"a better modeling solution. The prefered method for Source Engine is using 3dsmax with WallWorm Model Tools" +
                $" If you don't want to learn 3dsmax and WWMT, you can learn to configure propper at the link below.: " + 
                $"\n\nhttps://www.tophattwaffle.com/configuring-propper-for-steampipe/"
            };

            message.Channel.SendMessageAsync("", false, builder);

            return Task.CompletedTask;
        }

        private static Task VB(SocketMessage message)
        {
            Program.ChannelLog($"{message.Author} posted about Velocity Brawl #{message.Channel}. You should go kill them.");
            message.DeleteAsync(); //Delete their message about shit game
            var authBuilder = new EmbedAuthorBuilder()
            {
                Name = $"Hey there {message.Author.Username}!",
                IconUrl = message.Author.GetAvatarUrl(),
            };

            var builder = new EmbedBuilder()
            {
                Author = authBuilder,

                Title = $"Please no...",
                ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
                Color = new Color(243, 128, 72),

                Description = $"I saw you posted about Velocity Brawl. How about we do not do that."
            };
            
            message.Channel.SendMessageAsync("", false, builder);

            return Task.CompletedTask;
        }

        private static Task DeYork(SocketMessage message)
        {
            Random _rand = new Random();
            string[] yorkUrls = new string[]{
                "https://content.tophattwaffle.com/BotHATTwaffle/york/20161014230815_1.jpg",
                "https://content.tophattwaffle.com/BotHATTwaffle/york/20161014230840_1.jpg",
                "https://content.tophattwaffle.com/BotHATTwaffle/york/20161014230850_1.jpg",
                "https://content.tophattwaffle.com/BotHATTwaffle/york/20161014230941_1.jpg",
                "https://content.tophattwaffle.com/BotHATTwaffle/york/20161014231005_1.jpg",
                "https://content.tophattwaffle.com/BotHATTwaffle/york/20161014231026_1.jpg",
                "https://content.tophattwaffle.com/BotHATTwaffle/york/20161014231046_1.jpg",
                "https://content.tophattwaffle.com/BotHATTwaffle/york/20161014231116_1.jpg",
                "https://content.tophattwaffle.com/BotHATTwaffle/york/20161014231156_1.jpg",
                "https://content.tophattwaffle.com/BotHATTwaffle/york/20161014231204_1.jpg",
            };

            Program.ChannelLog($"{message.Author} posted about de_york #{message.Channel}. You should go meme them.");
            var authBuilder = new EmbedAuthorBuilder()
            {
                Name = $"Hey there {message.Author.Username}!",
                IconUrl = message.Author.GetAvatarUrl(),
            };
            
            var builder = new EmbedBuilder()
            {
                Author = authBuilder,
                Title = $"You talking about the best level ever?",
                
                ImageUrl = yorkUrls[_rand.Next(0, yorkUrls.Length)],
                Color = new Color(243, 128, 72),

                Description = $"I see that we both share the same love for amazing levels."
            };

            message.Channel.SendMessageAsync("", false, builder);

            return Task.CompletedTask;
        }
    }
}
