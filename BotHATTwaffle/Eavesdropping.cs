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
        Random _random;
        DataServices _dataServices;

        public Eavesdropping(DataServices dataService)
        {
            _dataServices = dataService;
            _random = new Random();

            _dataServices.agreeStrings = new string[]{
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
                        u.User.AddRoleAsync(_dataServices.playTesterRole);
                        u.User.SendMessageAsync("", false, u.JoinMessage);
                        joinDelayList.Remove(u);
                        _dataServices.ChannelLog($"{u.User} now has playtester role. Welcome DM was sent.");
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
            _dataServices.ChannelLog($"USER JOINED {inUser}", $"I will apply a roles at {inRoleTime}. They will then have playtester and can talk." +
                $"\nCreated At: {inUser.CreatedAt}" +
                $"\nJoined At: {inUser.JoinedAt}" +
                $"\nUser ID: {inUser.Id}");
            joinDelayList.Add(new UserData()
            {
                User = inUser,
                JoinRoleTime = inRoleTime,
                JoinMessage = message,
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
            //If the message is from a bot, just return.
            if (message.Author.IsBot)
                return;

            if (message.Content.Contains(":KMS:") || message.Content.Contains(":ShootMyself:") || message.Content.Contains(":HangMe:"))
            {
                var builder = new EmbedBuilder()
                {
                    ThumbnailUrl = "https://content.tophattwaffle.com/BotHATTwaffle/doit.jpg",
                };
                await message.Channel.SendMessageAsync("",false, builder);
                return;
            }
            if (message.Content.ToLower().Contains("who is daddy") || message.Content.ToLower().Contains("who is tophattwaffle"))
            {
                await message.Channel.SendMessageAsync("TopHATTwaffle my daddy.");
                return;
            }
            if (message.Content.ToLower().Contains("execute order 66"))
            {
                await message.Channel.SendMessageAsync("Yes my lord.");
                await message.Author.SendMessageAsync("Master Skywalker, there are too many of them. What are we going to do?");
                return;
            }
            foreach (string s in _dataServices.agreeEavesDrop)
            {
                if (message.Content.Equals("^") && message.Author.Username.Equals(s))
                {
                        await message.Channel.SendMessageAsync(_dataServices.agreeStrings[_random.Next(0, _dataServices.agreeStrings.Length)]);
                        return;
                }
            }
            foreach (string s in _dataServices.pakRatEavesDrop)
            {
                if(message.Content.ToLower().Contains(s))
                {
                    await PakRat(message);
                        return;
                }
            }
            foreach (string s in _dataServices.howToPackEavesDrop)
            {
                if (message.Content.ToLower().Contains(s))
                {
                    await HowToPack(message);
                    return;
                }
            }
            foreach (string s in _dataServices.carveEavesDrop)
            {
                if (message.Content.ToLower().Contains(s))
                {
                    await Carve(message);
                    return;
                }
            }
            foreach (string s in _dataServices.propperEavesDrop)
            {
                if (message.Content.ToLower().Contains(s))
                {
                    await Propper(message);
                    return;
                }
            }
            foreach (string s in _dataServices.vbEavesDrop)
            {
                if (message.Content.ToLower().Contains(s))
                {
                    await VB(message);
                    return;
                }
            }
            foreach (string s in _dataServices.yorkEavesDrop)
            {
                if (message.Content.ToLower().Contains(s))
                {
                    await DeYork(message);
                    return;
                }
            }
            if (message.Content.ToLower().Contains(_dataServices.tanookiEavesDrop))
            {
                await Tanooki(message);
                return;
            }
        }

        private Task PakRat(SocketMessage message)
        {
            _dataServices.ChannelLog($"{message.Author} was asking about PakRat in #{message.Channel}");

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

        private Task HowToPack(SocketMessage message)
        {
            _dataServices.ChannelLog($"{message.Author} was asking how to pack a level in #{message.Channel}");

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

        private Task Carve(SocketMessage message)
        {
            _dataServices.ChannelLog($"{message.Author} was asking how to carve in #{message.Channel}. You should probably kill them.");

            var authBuilder = new EmbedAuthorBuilder()
            {
                Name = $"Hey there {message.Author.Username}!",
                IconUrl = message.Author.GetAvatarUrl(),
            };

            var builder = new EmbedBuilder()
            {
                Author = authBuilder,

                Title = $"DO NOT USE CARVE",
                //ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
                Color = new Color(243, 128, 72),

                Description = $"I was minding my own damn business when you come around asking how to carve." +
                $"\n**__DON'T__**"
            };

            message.Channel.SendMessageAsync("", false, builder);

            return Task.CompletedTask;
        }

        private Task Propper(SocketMessage message)
        {
            _dataServices.ChannelLog($"{message.Author} was asking about Propper in #{message.Channel}. You should go WWMT fanboy.");

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

                Description = $"I saw you were asking about propper. While Propper still works, it's advised to learn " +
                $"a better modeling solution. The preferred method for Source Engine is using 3dsmax with WallWorm Model Tools" +
                $" If you don't want to learn 3dsmax and WWMT, you can learn to configure propper at the link below.: " + 
                $"\n\nhttps://www.tophattwaffle.com/configuring-propper-for-steampipe/"
            };

            message.Channel.SendMessageAsync("", false, builder);

            return Task.CompletedTask;
        }

        private Task VB(SocketMessage message)
        {
            _dataServices.ChannelLog($"{message.Author} posted about Velocity Brawl #{message.Channel}. You should go kill them.");
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
                //ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
                Color = new Color(243, 128, 72),

                Description = $"I saw you posted about Velocity Brawl. How about we do not do that."
            };
            
            message.Channel.SendMessageAsync("", false, builder);

            return Task.CompletedTask;
        }

        private Task DeYork(SocketMessage message)
        {
            _dataServices.ChannelLog($"{message.Author} posted about de_york #{message.Channel}. You should go meme them.");
            var authBuilder = new EmbedAuthorBuilder()
            {
                Name = $"Hey there {message.Author.Username}!",
                IconUrl = message.Author.GetAvatarUrl(),
            };
            
            var builder = new EmbedBuilder()
            {
                Author = authBuilder,
                Title = $"You talking about the best level ever?",
                
                ImageUrl = this._dataServices.GetRandomIMGFromUrl("https://content.tophattwaffle.com/BotHATTwaffle/york/"),
                Color = new Color(243, 128, 72),

                Description = $"I see that we both share the same love for amazing levels."
            };

            message.Channel.SendMessageAsync("", false, builder);

            return Task.CompletedTask;
        }

        private Task Tanooki(SocketMessage message)
        {
            _dataServices.ChannelLog($"{message.Author} posted about Tanooki #{message.Channel}. You should go meme them.");
            var authBuilder = new EmbedAuthorBuilder()
            {
                Name = $"Hey there {message.Author.Username}!",
                IconUrl = message.Author.GetAvatarUrl(),
            };

            var builder = new EmbedBuilder()
            {
                Author = authBuilder,
                Title = $"You talking about the worst csgo player ever?",

                ThumbnailUrl = this._dataServices.GetRandomIMGFromUrl("https://content.tophattwaffle.com/BotHATTwaffle/tanookifacts/"),
                Color = new Color(243, 128, 72),

                Description = $"I see that we both share the same love for terrible admins."
            };

            message.Channel.SendMessageAsync("", false, builder);

            return Task.CompletedTask;
        }
    }
}
