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
		private const int _JOIN_DELAY_ROLE_TIME = 10;
		private readonly List<UserData> _joinDelayList = new List<UserData>();
		private readonly Random _random;
		private readonly DataServices _dataServices;

		public Eavesdropping(DataServices dataService)
		{
			_dataServices = dataService;
			_random = new Random();

			_dataServices.AgreeStrings = new string[]{
				"^",
				"^^^",
				"^^^ I agree with ^^^",
			};

			//Start a timer. Starts after 10 seconds, and re-fires every 60 seconds.
			_timer = new Timer(_ =>
			{
				//Loop over all users in the join list.
				foreach (UserData u in _joinDelayList.ToList())
				{
					if (u.CanRole())
					{
						//Give them playtester role
						u.User.AddRoleAsync(_dataServices.PlayTesterRole);

						//Fire and forget to avoid compiler warning. We can't await this call because
						//we are not in an async method.
						Task fireAndForget = u.User.SendMessageAsync("", false, u.JoinMessage);

						//Remove them from the list
						_joinDelayList.Remove(u);

						//Log
						_dataServices.ChannelLog($"{u.User} now has playtester role. Welcome DM was sent.");
					}
				}
			},
			null,
			TimeSpan.FromSeconds(10),
			TimeSpan.FromSeconds(60));
		}

		/// <summary>
		/// Adds a user to the join list.
		/// </summary>
		/// <param name="inUser">User that joined</param>
		/// <param name="inRoleTime">What time can they get processed</param>
		/// <param name="message">Message to send user after they are processed</param>
		public void AddNewUserJoin(SocketGuildUser inUser, DateTime inRoleTime, Embed message)
		{
			//Log the user join
			_dataServices.ChannelLog($"USER JOINED {inUser}", $"I will apply a roles at {inRoleTime}. They will then have playtester and can talk." +
				$"\nCreated At: {inUser.CreatedAt}" +
				$"\nJoined At: {inUser.JoinedAt}" +
				$"\nUser ID: {inUser.Id}");

			//Add them to the list
			_joinDelayList.Add(new UserData()
			{
				User = inUser,
				JoinRoleTime = inRoleTime,
				JoinMessage = message,
			});
		}

		/// <summary>
		/// Run when the UserJoined event is raised.
		/// </summary>
		/// <param name="user">User that raised event</param>
		/// <returns></returns>
		internal async Task UserJoin(SocketUser user)
		{
			var authBuilder = new EmbedAuthorBuilder()
			{
				Name = $"Hey there {user.Username}! Welcome to the r/sourceengine discord server!",
				IconUrl = "https://cdn.discordapp.com/icons/111951182947258368/0e82dec99052c22abfbe989ece074cf5.png"
			};

			var footBuilder = new EmbedFooterBuilder()
			{
				Text = "Once again thanks for joining, hope you enjoy your stay!",
				IconUrl = Program.Client.CurrentUser.GetAvatarUrl()
			};

			var builder = new EmbedBuilder()
			{
				Author = authBuilder,
				Footer = footBuilder,

				Title = $"Thanks for joining!",
				Url = "https://www.tophattwaffle.com",
				ImageUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/12/Discord-LogoWordmark-Color.png",
				ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
				Color = new Color(243, 128, 72),

				Description = $"Hi there! Thanks for joining the SourceEngine Discord server!\n" +
							  $"Now that the {_JOIN_DELAY_ROLE_TIME} minute verification has ended, there are a few things I wanted to tell you! Feel free to ask a question in " +
							  $"any of the relevant channels you see. Just try to keep things on topic. \n\nAdditionally, you've been given a role called" +
							  $" `Playtester`. This role is used to notify you when we have a playtest starting. You can remove yourself from the " +
							  $"notifications by typing: `>playtester`.\n\nIf you want to see any of my commands, type: `>help`. Thanks for reading," +
							  $" and we hope you enjoy your stay here!" +
							  $"\n\nThere are roles you can use to show what skills you have. To see what roles you can give yourself, type: `>roleme display`" +
							  $"\n\nGLHF"
			};

			//Get the time they can be processed
			DateTime roleTime = DateTime.Now.AddMinutes(_JOIN_DELAY_ROLE_TIME);
			builder.Build();

			//Send them off to be added
			AddNewUserJoin((SocketGuildUser)user, roleTime, builder.Build());
		}

        internal async Task<bool> HandleWorkshopEmbeds(SocketMessage msg)
        {
            string content = msg.Content.Trim().ToLower();

            string fileDetails = "://steamcommunity.com/sharedfiles/filedetails/?id=";
            string workshop = "://steamcommunity.com/workshop/filedetails/?id=";

            int idStartPos = -1;
            int index;

            if ((index = content.IndexOf(fileDetails)) != -1)
                idStartPos = index + fileDetails.Length;
            else if ((index = content.IndexOf(workshop)) != -1)
                idStartPos = index + workshop.Length;

            if (idStartPos == -1)
                return false;

            string id = content.Substring(idStartPos);

            int spaceIndex = id.IndexOf(" ");
            if (spaceIndex != -1)
                id = id.Substring(0, spaceIndex);

            string workshopUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id=" + id;
            Summer.WorkshopItem item = new Summer.WorkshopItem();
            await item.Load(workshopUrl);

            if (!item.IsValid)
                return false;

            EmbedBuilder builder = new EmbedBuilder();
            builder.WithImageUrl(item.Image);
            builder.WithAuthor(item.AuthorName, item.AuthorImageUrl, item.AuthorUrl);
            builder.AddField("Game", item.AppName, true);
            string type = Enum.GetName(typeof(Summer.WorkshopItem.ItemType), item.Type);
            if (type == "Mod")
                type = "Map/Mod";
            builder.AddField("Type", type, true);
            builder.AddField("Tags", item.Tags.Aggregate((i, j) => i + ", " + j), true);
            builder.AddField("Description", item.Description.Length > 497 ? item.Description.Substring(0, 497) + "..." : item.Description);
            builder.WithUrl(item.Url);
            builder.WithColor(new Color(52, 152, 219));
            builder.WithTitle(item.Title);

            await msg.Channel.SendMessageAsync("", false, builder.Build());
            return true;
        }

		/// <summary>
		/// This is used to scan each message for less important things.
		/// Mostly used for shit posting, but also does useful things like nag users
		/// to use more up to date tools, or automatically answer some simple questions.
		/// </summary>
		/// <param name="message">Message that got us here</param>
		/// <returns></returns>
		internal async Task Listen(SocketMessage message)
		{
			//If the message is from a bot, just return.
			if (message.Author.IsBot)
				return;

            if (await HandleWorkshopEmbeds(message))
                return;

            //Is a shit post.
            if (message.Content.Contains(":KMS:") || message.Content.Contains(":ShootMyself:") || message.Content.Contains(":HangMe:"))
			{
				var builder = new EmbedBuilder()
				{
					ThumbnailUrl = "https://content.tophattwaffle.com/BotHATTwaffle/doit.jpg",
				};
				await message.Channel.SendMessageAsync("",false, builder);
				return;
			}

			//Is a shit post.
			if (message.Content.ToLower().Contains("who is daddy") || message.Content.ToLower().Contains("who is tophattwaffle"))
			{
				await message.Channel.SendMessageAsync("TopHATTwaffle my daddy.");
				return;
			}

			//Is a shit post.
			if (message.Content.ToLower().Contains("execute order 66"))
			{
				await message.Channel.SendMessageAsync("Yes my lord.");
				await message.Author.SendMessageAsync("Master Skywalker, there are too many of them. What are we going to do?");
				return;
			}

			//Is a shit post.
			if (this._dataServices.AgreeEavesDrop.Any( s => message.Content.Equals("^") && message.Author.Username.Equals(s) ||
					 ((SocketGuildUser)message.Author).Roles.Contains(this._dataServices.PatreonsRole) && message.Content.Equals("^")))
			{
				await message.Channel.SendMessageAsync(
					this._dataServices.AgreeStrings[this._random.Next(0, this._dataServices.AgreeStrings.Length)]);

				return;
			}

			if (_dataServices.PakRatEavesDrop.Any(s => message.Content.ToLower().Contains(s)))
			{
				await PakRat(message);

				return;
			}

			if (_dataServices.HowToPackEavesDrop.Any(s => message.Content.ToLower().Contains(s)))
			{
				await HowToPack(message);

				return;
			}

			if (_dataServices.CarveEavesDrop.Any(s => message.Content.ToLower().Contains(s)))
			{
				await Carve(message);

				return;
			}

			if (_dataServices.PropperEavesDrop.Any(s => message.Content.ToLower().Contains(s)))
			{
				await Propper(message);

				return;
			}

			if (_dataServices.VbEavesDrop.Any(s => message.Content.ToLower().Contains(s)))
			{
				await VB(message);

				return;
			}

			if (_dataServices.YorkEavesDrop.Any(s => message.Content.ToLower().Contains(s)))
			{
				await DeYork(message);

				return;
			}

			//Is a shit post.
			if (message.Content.ToLower().Contains(_dataServices.TanookiEavesDrop))
			{
				await Tanooki(message);

				return;
			}
		}

		/// <summary>
		/// Nags users to not use pakrat.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
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

		/// <summary>
		/// Tells users how to pack custom content.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
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

		/// <summary>
		/// Shames users for asking about carve.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
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

		/// <summary>
		/// Suggests WWMT over Propper
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
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

		/// <summary>
		/// No.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
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

		/// <summary>
		/// de_york really is the best level
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
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

				ImageUrl = _dataServices.GetRandomImgFromUrl("https://content.tophattwaffle.com/BotHATTwaffle/york/"),
				Color = new Color(243, 128, 72),

				Description = $"I see that we both share the same love for amazing levels."
			};

			message.Channel.SendMessageAsync("", false, builder);

			return Task.CompletedTask;
		}

		/// <summary>
		/// Big AND true.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
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

				ThumbnailUrl = _dataServices.GetRandomImgFromUrl("https://content.tophattwaffle.com/BotHATTwaffle/tanookifacts/"),
				Color = new Color(243, 128, 72),

				Description = $"I see that we both share the same love for terrible admins."
			};

			message.Channel.SendMessageAsync("", false, builder);

			return Task.CompletedTask;
		}
	}
}
