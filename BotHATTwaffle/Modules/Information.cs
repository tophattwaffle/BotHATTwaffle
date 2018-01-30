using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace BotHATTwaffle.Modules
{
	public class InformationModule : ModuleBase<SocketCommandContext>
	{
		private readonly DiscordSocketClient _client;
		private readonly DataServices _dataServices;
		private readonly Random _random;

		public InformationModule(DiscordSocketClient client, DataServices data, Random random)
		{
			_client = client;
			_dataServices = data;
			_random = random;
		}

		[Command("vdc")]
		[Summary("`>vdc [Search]` Quick link back to a VDC search.")]
		[Remarks(
			"Searches the Valve Developer Community and returns a link to the results. Try to use the proper full term; for " +
			"instance, `func_detail` will return better results than `detail`.")]
		[Alias("v")]
		public async Task SearchAsync([Remainder] string searchTerm)
		{
			await Context.Channel.TriggerTypingAsync();

			searchTerm = searchTerm.Replace(' ', '+');
			string builtUrl = $"https://developer.valvesoftware.com/w/index.php?search={searchTerm}&title=Special%3ASearch&go=Go";
			string siteTitle;

			// Download web page title and store to string.
			using (var client = new WebClient())
			{
				siteTitle = client.DownloadString(builtUrl);
			}

			var regex = new Regex(@"(?<=<title.*>)([\s\S]*)(?=</title>)", RegexOptions.IgnoreCase);
			siteTitle = regex.Match(siteTitle).Value.Trim();

			// Defaults the URL if it isn't properly formatted.
			if (!Uri.IsWellFormedUriString(builtUrl, UriKind.Absolute))
			{
				builtUrl = "https://developer.valvesoftware.com/wiki/Main_Page";
				searchTerm = "Valve Developer Community";
			}

			var builder = new EmbedBuilder
			{
				Author = new EmbedAuthorBuilder
				{
					Name = $"This is what I was able to find for {searchTerm}",
					IconUrl = "https://cdn.discordapp.com/icons/111951182947258368/0e82dec99052c22abfbe989ece074cf5.png"
				},
				Footer = new EmbedFooterBuilder
				{
					Text = "Thanks for using the VDC search!",
					IconUrl = _client.CurrentUser.GetAvatarUrl()
				},
				Title = "**Search Results**",
				Url = builtUrl,
				ImageUrl = "https://developer.valvesoftware.com/w/skins/valve/images-valve/logo.png",
				Color = new Color(71, 126, 159),
				Description = siteTitle
			};

			await ReplyAsync(string.Empty, false, builder.Build());
		}

		[Command("search", RunMode = RunMode.Async)]
		[Summary("`>search [series] [SearchTerm]` searches a tutorial series.")]
		[Remarks(
			"`>search [series] [SearchTerm]` searches our tutorial database for a result.\n" +
			"There are several series which can be searched. `>tutorial all [SearchTerm]` can be used to search them all; " +
			"`all` does not search the FAQ.\n" +
			"Example:\n" +
			"`>search v2 displacements` or `>search f leak`\n" +
			"`1` `V2Series` `v2`\n" +
			"`2` `CSGOBootcamp` `bc` `csgobootcamp`\n" +
			"`3` `3dsmax` `3ds`\n" +
			"`4` `WrittenTutorials` `written`\n" +
			"`5` `LegacySeries` `v1` `lg`\n" +
			"`6` `HammerTroubleshooting` `ht` `misc`\n" +
			"`7` `FAQ` `f`\n\n" +
			"`>s [series] [dump/all]` Example: `>s v2 all` Will display ALL tutorials in that series. It can be slow to " +
			"respond, so please wait!\n" +
			"Really big thanks to Mark for helping make the JSON searching work!")]
		[Alias("s")]
		public async Task SearchAsync(string series, [Remainder]string search)
		{
			IUserMessage wait = await ReplyAsync(
				$":eyes: Searching for **{search}** in **{series}**. This may take a moment! :eyes:");

			bool isPrivate = Context.IsPrivate;
			List<List<string>> results = _dataServices.Search(series, search, isPrivate); // Peforms a search.

			await _dataServices.ChannelLog($"{Context.User} ran a search", $"Series: {series}\nSearch Term: {search}");

			// A dump was requested.
			if (search.Equals("dump", StringComparison.InvariantCultureIgnoreCase) ||
			    search.Equals("all", StringComparison.InvariantCultureIgnoreCase))
			{
				// [0]title
				// [1]url
				// [2]tags
				var reply = new List<string>();
				string text = null;

				foreach (List<string> r in results)
				{
					text += $"[{r[0]}]({r[1]})\nTags: {r[2]}\n\n";

					if (text.Length <= 1800) continue;

					reply.Add(text);
					text = null;
				}

				if (!string.IsNullOrEmpty(text))
					reply.Add(text);

				try
				{
					foreach (string s in reply)
					{
						var embed = new EmbedBuilder {Color = new Color(243, 128, 72), Description = s};
						await Context.User.SendMessageAsync(string.Empty, false, embed.Build());
					}
				}
				catch
				{
					// Notifies a user of a failure to send them a DM.
					await ReplyAsync(
						"```\"dump\" and \"all\" search terms can ONLY send replies in a DM. This is to prevent flooding chat." +
						" You got this message because you do not accept DMs from non-friends.```");
				}

				if (!isPrivate)
				{
					await wait.DeleteAsync();
					await Context.Message.DeleteAsync(); // Delete the invoking message.
				}

				return;
			}

			// Notifies the user of a lack of search results.
			if (!results.Any())
			{
				results.Add(
					new List<string>
					{
						"Try a different search term",
						"http://tophattwaffle.com/faq",
						"I could not locate anything for the search term you provided. Please try a different search term.",
						null
					});
			}

			foreach (List<string> r in results)
			{
				var embed = new EmbedBuilder
				{
					Author = new EmbedAuthorBuilder
					{
						Name = r[0],
						IconUrl = "https://cdn.discordapp.com/icons/111951182947258368/0e82dec99052c22abfbe989ece074cf5.png"
					},
					Title = "Click Here",
					Url = r[1],
					ThumbnailUrl = r[3],
					Color = new Color(243, 128, 72),
					Description = r[2]
				};

				await ReplyAsync(string.Empty, false, embed.Build());
			}

			if (!isPrivate)
				await wait.DeleteAsync();
		}

		[Command("tutorials")]
		[Summary("`>tutorials [Optional series]` Displays links to tutorial series.")]
		[Remarks(
			"`>tutorials [Optional series]` Example: `>tutorials` `>tutorials v2`\n" +
			"Displays information about all tutorial series, or the specific one you're looking for\n\n" +
			"`1` `V2Series` `v2`\n" +
			"`2` `CSGOBootcamp` `bc` `csgobootcamp`\n" +
			"`3` `3dsmax` `3ds`\n" +
			"`4` `WrittenTutorials` `written`\n" +
			"`5` `LegacySeries` `v1` `lg`\n" +
			"`6` `HammerTroubleshooting` `ht`")]
		[Alias("t")]
		public async Task TutorialsAsync(string searchSeries = "all")
		{
			string authTitle;
			string bodyUrl;
			string bodyDescription;

			switch (searchSeries.ToLower())
			{
				case "v2series":
				case "v2":
				case "1":
					authTitle = "Version 2 Tutorial Series";
					bodyUrl = "https://goo.gl/XoVXzd";
					bodyDescription = "The Version 2 Tutorial series was created with the knowledge that I gained from " +
					                  "creating the Version 1 (now legacy) series of tutorials. The goal is to help someone " +
					                  "who hasn’t ever touched the tools get up and running in Source level design. You can " +
					                  "watch them in any order, but they have been designed to build upon each other.";

					break;
				case "csgobootcamp":
				case "bc":
				case "2":
					authTitle = "CSGO Level Design Bootcamp";
					bodyUrl = "https://goo.gl/srFBxe";
					bodyDescription = "The CSGO Boot Camp series was created for ECS to air during their Twitch streams " +
					                  "between matches. It is created to help someone with no experience with the level " +
					                  "design tools learn everything they need to create a competitive CSGO level. Most these " +
					                  "tutorials apply to every Source game, but a handful are specific to CSGO.";

					break;
				case "3dsmax":
				case "3ds":
				case "3":
					authTitle = "3ds Max Tutorials";
					bodyUrl = "https://goo.gl/JGg48X";
					bodyDescription = "There are a few sub series in the 3ds Max section. If you’re looking to create and " +
					                  "export your very first Source prop, check out the **My First Prop** series.\n" +
									  "If you’re getting start with 3ds Max look at the **Beginners Guide** series, which is " +
					                  "like the Version 2 Tutorial series but for 3ds Max.\nThere are a few one-off " +
					                  "tutorials listed on the page as well covering WallWorm functions";

					break;
				case "writtentutorials":
				case "written":
				case "4":
					authTitle = "Written Tutorials";
					bodyUrl = "https://goo.gl/i4aAqh";
					bodyDescription = "My library of written tutorials is typically about 1 off things that I want to cover. " +
					                  "They are usually independent of any specific game.";

					break;
				case "legacyseries":
				case "v1":
				case "lg":
				case "5":
					authTitle = "Legacy Series";
					bodyUrl = "https://goo.gl/aHFcvX";
					bodyDescription = "Hammer Troubleshooting is a smaller series that is created off user questions that I " +
					                  "see come up quite often.y are usually independent of any specific game.";

					break;
				case "hammertroubleshooting":
				case "ht":
				case "6":
					authTitle = "Hammer Troubleshooting";
					bodyUrl = "https://goo.gl/tBh7jT";
					bodyDescription = "The First tutorial series was my launching point for getting better at mapping. Not " +
					                  "only did I learn a lot from making it, but I like to think that many others learned " +
					                  "something from the series as well. The series was flawed in that it was not " +
					                  "structured, and lacked quality control. But you may notice that the further along in " +
					                  "the series you are, the better quality they get. Example is the 100th tutorial, it " +
					                  "heavily reflects how the V2 series was created. You can view the entire series below. " +
					                  "Just be warned that some of the information in these videos may not be correct, or " +
					                  "even work any longer. Please watch at your own risk. I attempt to support these " +
					                  "tutorials, but cannot due to time. Please watch the V2 series";

					break;
				case "all":
					authTitle = "All Tutorial Series Information";
					bodyUrl = "https://www.tophattwaffle.com/tutorials/";
					bodyDescription = "Over the years I've built up quite the collection of tutorial series!\n" +
									  "_Here they all are__\n_" +
									  "[Version 2 Series](https://goo.gl/XoVXzd)\n_" +
									  "[CSGO Bootcamp](https://goo.gl/srFBxe)\n_" +
									  "[3ds Max](https://goo.gl/JGg48X)\n_" +
									  "[Written Tutorials](https://goo.gl/i4aAqh)\n_" +
									  "[Hammer Troubleshooting](https://goo.gl/tBh7jT)\n_" +
									  "[Legacy Series V1](https://goo.gl/aHFcvX)";

					break;
				default:
					await ReplyAsync("Unknown series. Please try `>help tutorials` to see all the options.");
					return;
			}

			var embed = new EmbedBuilder
			{
				Title = "Click Here!",
				Url = bodyUrl,
				ImageUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/header.png",
				Color = new Color(243,128,72),

				Description = bodyDescription
			};

			embed.WithAuthor(authTitle, "https://cdn.discordapp.com/icons/111951182947258368/0e82dec99052c22abfbe989ece074cf5.png");
			embed.WithFooter(null, _client.CurrentUser.GetAvatarUrl());

			await ReplyAsync(string.Empty, false, embed.Build());
		}

		[Command("CatFact", RunMode = RunMode.Async)]
		[Summary("`>catFact` Provides a cat fact!")]
		[Remarks("Ever want to know more about cats? Now you can.")]
		[Alias("gimme a cat fact", "hit me with a cat fact", "hit a nigga with a cat fact", "cat fact", "CatFacts", "cat facts")]
		public async Task CatFactAsync()
		{
			var catFact = "Did you know cats have big bushy tails?";
			var name = "Cat Fact 0";

			// Gets a fact from the file.
			if (File.Exists(_dataServices.CatFactPath))
			{
				string[] allLines = File.ReadAllLines(_dataServices.CatFactPath);
				int lineNumber = _random.Next(0, allLines.Length);
				catFact = allLines[lineNumber];

				// Splits the name and the fact in the selected line.
				Match match = Regex.Match(catFact, @"^\w+ Fact \d*", RegexOptions.IgnoreCase);
				name = match.Value;
				catFact = catFact.Substring(match.Length).Trim();
			}

			var embed = new EmbedBuilder
			{
				ThumbnailUrl = _dataServices.GetRandomImgFromUrl("https://content.tophattwaffle.com/BotHATTwaffle/catfacts/"),
				Color = new Color(230, 235, 240)
			};

			embed.WithAuthor("CAT FACTS!", Context.Message.Author.GetAvatarUrl());
			embed.WithFooter("This was cat facts, you cannot unsubscribe.");
			embed.AddField(name, catFact);

			await _dataServices.ChannelLog($"{Context.Message.Author.Username.ToUpper()} JUST GOT HIT WITH A CAT FACT");
			await ReplyAsync(string.Empty, false, embed.Build());
		}

		[Command("unsubscribe")]
		[Summary("`>unsubscribe` Unsubscribes the invoking user from cat facts.")]
		[Remarks("Takes the invoking user off the cat fact list.")]
		public async Task CatFactUnsubAsync() => await ReplyAsync("You cannot unsubscribe from cat facts...");

		[Command("PenguinFact", RunMode = RunMode.Async)]
		[Summary("`>PenguinFact` Provides a Penguin fact!")]
		[Remarks("Ever want to know more about Penguin? Now you can.")]
		[Alias("gimme a penguin fact", "hit me with a penguin fact", "hit a nigga with a penguin fact", "penguin fact",
			"PenguinFacts", "penguin facts")]
		public async Task PenguinFactAsync()
		{
			var penguinFact = "Did you know penguins have big bushy tails?";

			// Gets a fact from the file.
			if (File.Exists(_dataServices.PenguinFactPath))
			{
				string[] allLines = File.ReadAllLines(_dataServices.PenguinFactPath);
				int lineNumber = _random.Next(0, allLines.Length);
				penguinFact = allLines[lineNumber];
			}

			var embed = new EmbedBuilder
			{
				ThumbnailUrl = _dataServices.GetRandomImgFromUrl("https://content.tophattwaffle.com/BotHATTwaffle/penguinfacts/"),
				Color = new Color(230, 235, 240),
				Description = penguinFact
			};

			embed.WithAuthor("PENGUIN FACTS!", Context.Message.Author.GetAvatarUrl());
			embed.WithFooter("This was penguin facts; you cannot unsubscribe.");

			await _dataServices.ChannelLog($"{Context.Message.Author.Username.ToUpper()} JUST GOT HIT WITH A PENGUIN FACT");
			await ReplyAsync(string.Empty, false, embed.Build());
		}

		[Command("TanookiFact", RunMode = RunMode.Async)]
		[Summary("`>tanookiFact` Provides a Tanooki fact!")]
		[Remarks("Ever want to know more about Tanooki? Now you can.")]
		[Alias("gimme a tanooki fact", "hit me with a tanooki fact", "hit a nigga with a tanooki fact", "tanooki fact",
			"TanookiFacts", "tanooki facts", "@TanookiSuit3")]
		public async Task TanookiFactAsync()
		{
			var tanookiFact = "Did you know Tanooki has a big bushy tail?";

			// Gets a fact from the file.
			if (File.Exists(_dataServices.TanookiFactPath))
			{
				string[] allLines = File.ReadAllLines(_dataServices.TanookiFactPath);
				int lineNumber = _random.Next(0, allLines.Length);
				tanookiFact = allLines[lineNumber];
			}

			var embed = new EmbedBuilder
			{
				ThumbnailUrl = _dataServices.GetRandomImgFromUrl("https://content.tophattwaffle.com/BotHATTwaffle/tanookifacts/"),
				Color = new Color(230, 235, 240),
				Description = tanookiFact
			};

			embed.WithAuthor("TANOOKI FACTS!", Context.Message.Author.GetAvatarUrl());
			embed.WithFooter("This was Tanooki facts; you cannot unsubscribe.");

			await _dataServices.ChannelLog($"{Context.Message.Author.Username.ToUpper()} JUST GOT HIT WITH A TANOOKI FACT");
			await ReplyAsync(string.Empty, false, embed.Build());
		}

		[Command("TanookiIRL", RunMode = RunMode.Async)]
		[Summary("`>tanookiirl` Displays Tanooki looking at stuff!")]
		[Alias("TanookiLooksAtThings")]
		public async Task TanookiLookAsync()
		{
			var embed = new EmbedBuilder
			{
				ImageUrl = _dataServices.GetRandomImgFromUrl("https://content.tophattwaffle.com/BotHATTwaffle/kimjongillookingatthings/"),
				Color = new Color(138, 43, 226)
			};

			await ReplyAsync(string.Empty, false, embed.Build());
		}
	}
}
