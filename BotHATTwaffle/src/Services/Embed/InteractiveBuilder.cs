using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using BotHATTwaffle.Extensions;

using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

namespace BotHATTwaffle.Services.Embed
{
    /// <summary>
    /// Interactively builds an embed by prompting the user.
    /// </summary>
    public class InteractiveBuilder
    {
        private static readonly string _Instructions =
            "Enter one of the following options:\n" +
            string.Join(", ", BuilderAction.Actions.Keys.OrderBy(k => k).Select(k => $"`{k}`")) +
            ", `Field`\n`Submit` to send the embed.\n`Cancel` to abort.";

        private readonly SocketCommandContext _context;
        private readonly InteractiveService _interactive;
        private readonly EmbedBuilder _embed;
        private readonly EmbedBuilder _embedLayout;

        private IUserMessage _layoutMsg;
        private IUserMessage _previewMsg;
        private IUserMessage _instructionsMsg;

        /// <summary>
        /// Constructs an interactive embed builder at the given context.
        /// </summary>
        /// <param name="context">The context in which the embed will be built.</param>
        /// <param name="interactive">The service used for retrieving next messages and deleting messages after a delay.</param>
        public InteractiveBuilder(SocketCommandContext context, InteractiveService interactive)
        {
            _context = context;
            _interactive = interactive;

            string avatar = _context.Message.Author.GetAvatarUrl();

            _embed = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder().WithIconUrl(avatar),
                Fields = new List<EmbedFieldBuilder>(),
                Footer = new EmbedFooterBuilder().WithIconUrl(avatar),
            };

            _embedLayout = new EmbedBuilder().WithImageUrl("https://content.tophattwaffle.com/BotHATTwaffle/embed.png");
        }

        /// <summary>
        /// Interactively builds an embed by prompting the user.
        /// <para>
        /// Shows an embed layout guide, a preview of the embed being built, and instructions for the current prompt. The user is
        /// first prompted for an action. Then, if applicable, the user is prompted again to enter a value for the action. The
        /// builder is cancelled if it times out before an initial response is received. Otherwise, it listens indefinitely for
        /// a response.
        /// </para>
        /// <para>See <see cref="_Instructions"/> and <see cref="BuilderAction.Actions"/> for supported actions.</para>
        /// </summary>
        /// <returns>The built embed.</returns>
        public async Task<Discord.Embed> BuildAsync()
        {
            await InitAsync();

            while (true)
            {
                SocketMessage input = await _interactive.NextMessageAsync(_context);

                if (input == null)
                {
                    await _context.Channel.SendMessageAsync("```The announcement builder has timed out after 120 seconds.```");
                    await RemoveAsync();

                    return null;
                }

                if (BuilderAction.Actions.TryGetValue(input.Content, out BuilderAction action))
                {
                    await input.DeleteAsync();
                    await _instructionsMsg.ModifyAsync(m => m.Content = action.Instructions);
                    input = await _interactive.NextMessageAsync(_context);

                    if (input == null) continue;

                    // Displays an error if the callback failed and the action has an error message.
                    if (!action.Callback(input.Content, _embed) && !string.IsNullOrWhiteSpace(action.Error))
                    {
                        await _interactive.ReplyAndDeleteAsync(
                            _context,
                            $"```Input: {input.Content}\nError: {action.Error}```",
                            timeout: TimeSpan.FromSeconds(5));
                    }

                    await input.DeleteAsync();
                }
                else if (input.Content.Equals("field", StringComparison.OrdinalIgnoreCase))
                {
                    await input.DeleteAsync();
                    await HandleFieldAsync();
                }
                else if (input.Content.Equals("submit", StringComparison.OrdinalIgnoreCase))
                {
                    await input.DeleteAsync();
                    await RemoveAsync();

                    return _embed.Build();
                }
                else if (input.Content.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                {
                    await input.DeleteAsync();
                    await RemoveAsync();

                    return null;
                }
                else
                {
                    await input.DeleteAsync();
                    await _interactive.ReplyAndDeleteAsync(
                        _context,
                        $"```Unknown action '{input.Content}'.```",
                        timeout: TimeSpan.FromSeconds(5));

                    continue;
                }

                await _previewMsg.ModifyAsync(m => m.Embed = _embed.Build());
                await _instructionsMsg.ModifyAsync(m => m.Content = _Instructions);
            }
        }

        /// <summary>
        /// Prompts for the channels to which to send the embed.
        /// <para>Prompts until timeout, explicitly cancelled, or the response contains valid channel mentions.</para>
        /// </summary>
        /// <param name="context">The context in which to send the prompts.</param>
        /// <returns>A collection of the destination channels.</returns>
        public async Task<IReadOnlyCollection<SocketTextChannel>> PromptDestinationAsync(SocketCommandContext context)
        {
            IUserMessage message =
                await context.Channel.SendMessageAsync("Mention the channels to which to send the embed or enter `cancel`:");

            while (true)
            {
                var response = await _interactive.NextMessageAsync(context) as SocketUserMessage;

                if (response == null)
                {
                    await context.Channel.SendMessageAsync("```The announcement builder timed out after 120 seconds.```");

                    return null;
                }

                if (response.Content.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                {
                    await response.AddReactionAsync(new Emoji("\U0001f44c")); // Reacts with ok_hand.

                    return null;
                }

                IReadOnlyCollection<SocketTextChannel> channels = response.GetChannelMentions();

                if (!channels.Any())
                {
                    await response.DeleteAsync();
                    await _interactive.ReplyAndDeleteAsync(
                        context,
                        "```No channel mentions were found; try again.```",
                        timeout: TimeSpan.FromSeconds(5));

                    continue;
                }

                await message.DeleteAsync();
                await response.DeleteAsync();

                return channels;
            }
        }

        /// <summary>
        /// Handles the addition of embed fields.
        /// <para>Addition is cancelled on timeout.</para>
        /// </summary>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        private async Task HandleFieldAsync()
        {
            // Name
            await _instructionsMsg.ModifyAsync(m => m.Content = "Enter the field's name:");
            SocketMessage input = await _interactive.NextMessageAsync(_context);

            if (input == null) return;

            string name = input.Content;
            await input.DeleteAsync();

            // Value
            await _instructionsMsg.ModifyAsync(m => m.Content = "Enter the field's content:");
            input = await _interactive.NextMessageAsync(_context);

            if (input == null) return;

            string value = input.Content;
            await input.DeleteAsync();

            // Inline
            await _instructionsMsg.ModifyAsync(m => m.Content = "Inline? [T]rue or [F]alse?");
            input = await _interactive.NextMessageAsync(_context);

            if (input == null) return;

            bool inline = input.Content.StartsWith("t", StringComparison.OrdinalIgnoreCase);
            await input.DeleteAsync();

            _embed.Fields.Add(new EmbedFieldBuilder { Name = name, Value = value, IsInline = inline });
        }

        /// <summary>
        /// Sends the initial messages before the builder prompts commence.
        /// </summary>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        private async Task InitAsync()
        {
            _layoutMsg = await _context.Channel.SendMessageAsync(string.Empty, false, _embedLayout.Build());
            _previewMsg = await _context.Channel.SendMessageAsync("__**PREVIEW**__");
            _instructionsMsg = await _context.Channel.SendMessageAsync(_Instructions);
        }

        /// <summary>
        /// Removes the initial sent messages.
        /// </summary>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        private async Task RemoveAsync()
        {
            await _layoutMsg.DeleteAsync();
            await _previewMsg.DeleteAsync();
            await _instructionsMsg.DeleteAsync();
        }
    }
}
