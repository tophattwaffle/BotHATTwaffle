#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BotHATTwaffle.Models;

using Discord;
using Discord.WebSocket;

using Summer;

namespace BotHATTwaffle.Services
{
	public class MessageListener
	{
		private readonly Timer _timer;
		private const int _JOIN_DELAY_ROLE_TIME = 10;
		private readonly List<UserData> _joinDelayList = new List<UserData>();
		private readonly DiscordSocketClient _client;
		private readonly DataService _dataService;
		private readonly Random _random;
		private DateTime _canShitPost;
		private readonly WorkshopItem _wsItem = new WorkshopItem();

		public MessageListener(DiscordSocketClient client, DataService dataService, Random random)
		{
			_client = client;
			_dataService = dataService;
			_random = random;

			_dataService.AgreeStrings = new string[]{
				"^",
				"^^^",
				"^^^ I agree with ^^^",
			};

			//Subtract value so we can shitpost once right away.
			_canShitPost = DateTime.Now.AddMinutes(_dataService.ShitPostDelay * -1);

			//Start a timer. Starts after 10 seconds, and re-fires every 60 seconds.
			_timer = new Timer(_ =>
			{
				//Loop over all users in the join list.
				foreach (UserData u in _joinDelayList.ToList())
				{
					if (u.CanHandleJoin())
					{
						//Give them playtester role
						u.User.AddRoleAsync(_dataService.PlayTesterRole);

						//Fire and forget to avoid compiler warning. We can't await this call because
						//we are not in an async method.
						Task fireAndForget = u.User.SendMessageAsync("", false, u.JoinMessage);

						//Remove them from the list
						_joinDelayList.Remove(u);

						//Log
						_dataService.ChannelLog($"{u.User} now has playtester role. Welcome DM was sent.");
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
		public void AddNewUserJoin(SocketGuildUser inUser, DateTime inRoleTime, Discord.Embed message)
		{
			//Log the user join
			_dataService.ChannelLog($"USER JOINED {inUser}", $"I will apply a roles at {inRoleTime}. They will then have playtester and can talk." +
				$"\nCreated At: {inUser.CreatedAt}" +
				$"\nJoined At: {inUser.JoinedAt}" +
				$"\nUser ID: {inUser.Id}");

			//Add them to the list
			_joinDelayList.Add(new UserData()
			{
				User = inUser,
				HandleJoinTime = inRoleTime,
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
				IconUrl = _client.Guilds.FirstOrDefault()?.IconUrl
			};

			var footBuilder = new EmbedFooterBuilder()
			{
				Text = "Once again thanks for joining, hope you enjoy your stay!",
				IconUrl = _client.CurrentUser.GetAvatarUrl()
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

		

		/// <summary>
		/// This is used to scan each message for less important things.
		/// Mostly used for shit posting, but also does useful things like nag users
		/// to use more up to date tools, or automatically answer some simple questions.
		/// </summary>
		/// <param name="message">Message that got us here</param>
		/// <returns></returns>
		internal async Task Listen(SocketMessage message)
		{
			if (message.Content.StartsWith("BOT_KEY-A2F3D6"))
			{
				await message.DeleteAsync();
				var msgSplit = message.Content.Split('|');

				var splitUser = msgSplit[1].Split('#');

				try
				{
					//Try to tag
					await message.Channel.SendMessageAsync($"New Playtest Request Submitted by {_client.GetUser(splitUser[0], splitUser[1]).Mention}, check it out!");
				}
				catch
				{
					//Can't tag
					await message.Channel.SendMessageAsync($"New Playtest Request Submitted by {msgSplit[1]}, check it out!");
				}
				await _wsItem.HandleWorkshopEmbeds(message, msgSplit[4], msgSplit[2]);

				return;
			}

			//If the message is from a bot, just return.
			if (message.Author.IsBot)
				return;

			//Is a shit post.
			if (CanShitPost())
			{
				if (message.Content.StartsWith("I'm", StringComparison.OrdinalIgnoreCase))
				{
					await message.Channel.SendMessageAsync($"Hi {message.Content.Substring(3).Trim()}, I'm BotHATTwaffle.");

					_canShitPost = DateTime.Now.AddMinutes(_dataService.ShitPostDelay);
					return;
				}
			}

			//Is a shit post.
			if (CanShitPost())
			{
				if (message.Content.Contains(":KMS:") || message.Content.Contains(":ShootMyself:") || message.Content.Contains(":HangMe:"))
				{
					var builder = new EmbedBuilder()
					{
						ThumbnailUrl = "https://content.tophattwaffle.com/BotHATTwaffle/doit.jpg",
					};
					await message.Channel.SendMessageAsync("",false, builder);
					_canShitPost = DateTime.Now.AddMinutes(_dataService.ShitPostDelay);
					return;
				}
			}

			//Is a shit post.
			if (CanShitPost())
			{
				if (message.Content.ToLower().Contains("who is daddy") || message.Content.ToLower().Contains("who is tophattwaffle"))
				{
					await message.Channel.SendMessageAsync("TopHATTwaffle my daddy.");
					_canShitPost = DateTime.Now.AddMinutes(_dataService.ShitPostDelay);
					return;
				}
			}

			//Is a shit post.
			if (CanShitPost())
			{
				if (message.Content.ToLower().Contains("sudo make me a sandwich"))
				{
					await message.Channel.SendMessageAsync("ok.");
					_canShitPost = DateTime.Now.AddMinutes(_dataService.ShitPostDelay);
					return;
				}
			}

			//Is a shit post.
			if (CanShitPost())
			{
				if (message.Content.ToLower().Contains("execute order 66"))
				{
					await message.Channel.SendMessageAsync("Yes my lord.");
					await message.Author.SendMessageAsync("Master Skywalker, there are too many of them. What are we going to do?");
					_canShitPost = DateTime.Now.AddMinutes(_dataService.ShitPostDelay);
					return;
				}
			}

			//Is a shit post.

			if (this._dataService.AgreeEavesDrop.Any( s => message.Content.Equals("^") && message.Author.Username.Equals(s) ||
					 ((SocketGuildUser)message.Author).Roles.Contains(this._dataService.PatreonsRole) && message.Content.Equals("^")))
			{
				await message.Channel.SendMessageAsync(
					this._dataService.AgreeStrings[this._random.Next(0, this._dataService.AgreeStrings.Length)]);

				return;
			}

			if (_dataService.PakRatEavesDrop.Any(s => message.Content.ToLower().Contains(s)))
			{
				await PakRat(message);

				return;
			}

			if (_dataService.HowToPackEavesDrop.Any(s => message.Content.ToLower().Contains(s)))
			{
				await HowToPack(message);

				return;
			}

			if (CanShitPost())
			{
				if (_dataService.CarveEavesDrop.Any(s => message.Content.ToLower().Contains(s)))
				{
					await Carve(message);
					_canShitPost = DateTime.Now.AddMinutes(_dataService.ShitPostDelay);
					return;
				}
			}

			if (_dataService.PropperEavesDrop.Any(s => message.Content.ToLower().Contains(s)))
			{
				await Propper(message);

				return;
			}

			if (CanShitPost())
			{
				if (_dataService.VbEavesDrop.Any(s => message.Content.ToLower().Contains(s)))
				{
					await VB(message);
					_canShitPost = DateTime.Now.AddMinutes(_dataService.ShitPostDelay);
					return;
				}
			}

			if (CanShitPost())
			{
				if (_dataService.YorkEavesDrop.Any(s => message.Content.ToLower().Contains(s)))
				{
					await DeYork(message);
					_canShitPost = DateTime.Now.AddMinutes(_dataService.ShitPostDelay);

					return;
				}
			}

			//Is a shit post.
			if (CanShitPost())
			{
				if (message.Content.ToLower().Contains(_dataService.TanookiEavesDrop))
				{
					await Tanooki(message);
					_canShitPost = DateTime.Now.AddMinutes(_dataService.ShitPostDelay);
					return;
				}
			}

			await _wsItem.HandleWorkshopEmbeds(message);
		}


		private bool CanShitPost() => _canShitPost < DateTime.Now;

		/// <summary>
		/// Nags users to not use pakrat.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		private Task PakRat(SocketMessage message)
		{
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

				Description = $"You were asking about carve. We don't use carve here. Not only does it create bad brushwork, but it " +
	$"can also cause Hammer to stop responding and crash. If you're here trying to defend using carve, just stop - you are wrong."
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
			var authBuilder = new EmbedAuthorBuilder()
			{
				Name = $"Hey there {message.Author.Username}!",
				IconUrl = message.Author.GetAvatarUrl(),
			};

			var builder = new EmbedBuilder()
			{
				Author = authBuilder,
				Title = $"You talking about the best level ever?",

				ImageUrl = _dataService.GetRandomImgFromUrl("https://content.tophattwaffle.com/BotHATTwaffle/york/"),
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
			var authBuilder = new EmbedAuthorBuilder()
			{
				Name = $"Hey there {message.Author.Username}!",
				IconUrl = message.Author.GetAvatarUrl(),
			};

			var builder = new EmbedBuilder()
			{
				Author = authBuilder,
				Title = $"You talking about the worst csgo player ever?",

				ThumbnailUrl = _dataService.GetRandomImgFromUrl("https://content.tophattwaffle.com/BotHATTwaffle/tanookifacts/"),
				Color = new Color(243, 128, 72),

				Description = $"I see that we both share the same love for terrible admins."
			};

			message.Channel.SendMessageAsync("", false, builder);

			return Task.CompletedTask;
		}
	}
}
