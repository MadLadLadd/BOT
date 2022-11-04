﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Reddit;
using Reddit.Exceptions;
using Post = Reddit.Controllers.Post;
using Subreddit = Reddit.Controllers.Subreddit;

namespace EconomyBot;

public class ImagesModule : BaseCommandModule {
    [Command("muv"), Description("Ghost's gay shit lol")]
    public async Task muvluv(CommandContext ctx, string luv) {
        if (luv == "luv") {
            // thank you ghost lol
            await ctx.TriggerTypingAsync();
            var imgProvider = new RedditImageProvider();
            await sendFancyEmbed(ctx, await imgProvider.getImageFromSub("muvluv"), "Muv-Luv!");
        }
    }

    [Command("yaoi"), Description("Fetch a yaoi image.")]
    public async Task yaoi(CommandContext ctx) {
        await ctx.TriggerTypingAsync();
        var imgProvider = new RedditImageProvider();
        await sendFancyEmbed(ctx, await imgProvider.getImageFromSub("yaoi"), "Cute boys!");
    }

    [Command("img"), Description("Fetch a wholesome image.")]
    public async Task img(CommandContext ctx) {
        await ctx.TriggerTypingAsync();
        var imgProvider = new RedditImageProvider();
        await sendFancyEmbed(ctx, await imgProvider.getRandomImage(), "Cute girls!");
    }

    public async Task sendFancyEmbed(CommandContext ctx, string url, string title) {
        // send the image
        var messageBuilder = new DiscordMessageBuilder().WithEmbed(new DiscordEmbedBuilder()
            .WithColor(DiscordColor.Rose).WithDescription(title).WithImageUrl(url));
        await ctx.RespondAsync(messageBuilder);
    }
}