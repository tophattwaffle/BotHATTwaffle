﻿using System.Collections.Generic;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;

namespace BotHATTwaffle.Modules
{
	/// <summary>
	/// Contains commands related to Source shader development.
	/// </summary>
	public class ShadersModule : ModuleBase<SocketCommandContext>
	{
		[Command("shaders")]
		[Summary("Provides links to Source shader development resources.")]
		[Alias("shader")]
		public async Task ShadersAsync()
		{
			var embed = new EmbedBuilder
			{
				Author = new EmbedAuthorBuilder
				{
					Name = "Shader Resources",
					IconUrl = "https://cdn.discordapp.com/icons/111951182947258368/0e82dec99052c22abfbe989ece074cf5.png"
				},
				Fields =
				{
					new EmbedFieldBuilder
					{
						Name = "General",
						Value = "[Shader](https://developer.valvesoftware.com/wiki/Shader)\n" +
						        "[Shader Authoring](https://developer.valvesoftware.com/wiki/Shader_Authoring)\n" +
						        "[Guide to Shaders](http://www.bit-tech.net/reviews/tech/guide_to_shaders/1/)\n" +
						        "[Introduction to Shaders](http://www.moddb.com/games/half-life-2/tutorials/introduction-to-shaders)\n" +
						        "[Post Process Shaders](http://www.moddb.com/games/half-life-2/tutorials/post-process-shader)",
						IsInline = false
					},
					new EmbedFieldBuilder
					{
						Name = "Language",
						Value = "[High-Level Shader Language](https://en.wikipedia.org/wiki/High-Level_Shading_Language)",
						IsInline = false
					},
					new EmbedFieldBuilder
					{
						Name = "Types",
						Value = "[Vertex](https://developer.valvesoftware.com/wiki/Shader#Vertex_shaders)\n" +
						        "[Pixel](https://developer.valvesoftware.com/wiki/Shader#Pixel_shaders)",
						IsInline = false
					}
				},
				Color = new Color(240, 235, 230),
				Description = "Here are lists of popular shader resources:"
			};

			await ReplyAsync(string.Empty, false, embed.Build());
		}
	}
}