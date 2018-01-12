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

	public class ShadersModule : ModuleBase<SocketCommandContext>
	{

		[Command("shader")]
		[Summary("`>shader` Gives you a shader information")]
		[Alias("shaders")]
		public async Task ShaderAsync()
		{
			List<EmbedFieldBuilder> fieldBuilder = new List<EmbedFieldBuilder>();

			var authBuilder = new EmbedAuthorBuilder()
			{
				Name = "Shader Resources",
				IconUrl = "https://cdn.discordapp.com/icons/111951182947258368/0e82dec99052c22abfbe989ece074cf5.png",
			};

			fieldBuilder.Add(new EmbedFieldBuilder{
				Name = "General Shader",
				Value = $"[General Shader](https://developer.valvesoftware.com/wiki/Shader)" +
						$"\n[Shader Authoring](https://developer.valvesoftware.com/wiki/Shader_Authoring)" +
				        $"\n[Guide to Shaders](http://www.bit-tech.net/reviews/tech/guide_to_shaders/1/)" +
				        $"\n[Introduction to Shaders](http://www.moddb.com/games/half-life-2/tutorials/introduction-to-shaders)" +
				        $"\n[Post Process Shaders](http://www.moddb.com/games/half-life-2/tutorials/post-process-shader)", IsInline = false });
			fieldBuilder.Add(new EmbedFieldBuilder
			{
				Name = "Shader Language",
				Value = $"[High-Level Shader Language](https://en.wikipedia.org/wiki/High-Level_Shading_Language)" + 
				        $"\n[OpenGL Shading Language](https://en.wikipedia.org/wiki/OpenGL_Shading_Language)",
				IsInline = false
			});
			fieldBuilder.Add(new EmbedFieldBuilder
			{
				Name = "Shader Types",
				Value = $"[Vertex](https://developer.valvesoftware.com/wiki/Shader#Vertex_shaders )" +
				        $"\n[Pixel](https://developer.valvesoftware.com/wiki/Shader#Pixel_shaders)",
				IsInline = false
			});

			var builder = new EmbedBuilder()
			{
				Author = authBuilder,
				Fields = fieldBuilder,
				Color = new Color(240, 235, 230),
				Description = "Here are a list of popular shader resources."

			};
			await ReplyAsync("", false, builder.Build());
		}
	}
}
