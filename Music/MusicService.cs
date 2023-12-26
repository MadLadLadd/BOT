﻿using System.Collections.Concurrent;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using Microsoft.EntityFrameworkCore;

namespace EconomyBot;

/// <summary>
/// Provides a persistent way of tracking music in various guilds.
/// </summary>
public sealed class MusicService {
    private LavalinkExtension Lavalink;
    private ConcurrentDictionary<ulong, GuildMusicData> MusicData;
    private readonly DiscordClient client;

    private LavalinkNodeConnection node { get; }

    /// <summary>
    /// Creates a new instance of this music service.
    /// </summary>
    public MusicService(LavalinkExtension lavalink, LavalinkNodeConnection theNode) {
        Lavalink = lavalink;
        MusicData = new ConcurrentDictionary<ulong, GuildMusicData>();
        client = lavalink.Client;
        node = theNode;

        node.TrackException += Lavalink_TrackExceptionThrown;

        async Task playbackStarted(LavalinkGuildConnection con, TrackStartEventArgs e) {
            await Console.Out.WriteLineAsync($"len/nodes: {lavalink.ConnectedNodes.Count}");
        }

        node.PlaybackStarted += playbackStarted;
    }

    /// <summary>
    /// Saves data for specified guild.
    /// </summary>
    /// <param name="guild">Guild to save data for.</param>
    /// <returns></returns>
    public void SaveDataForAsync(DiscordGuild guild) {
        MusicData.TryGetValue(guild.Id, out _);
    }

    /// <summary>
    /// Gets or creates a dataset for specified guild.
    /// </summary>
    /// <param name="guild">Guild to get or create dataset for.</param>
    /// <returns>Resulting dataset.</returns>
    public async Task<GuildMusicData> GetOrCreateDataAsync(DiscordGuild guild) {
        if (MusicData.TryGetValue(guild.Id, out var gmd))
            return gmd;

        gmd = MusicData.AddOrUpdate(guild.Id, new GuildMusicData(guild, Lavalink, node),
            (k, v) => v);

        await getDBForGuild(guild);
        gmd.setupWebhooks();

        return gmd;
    }

    public async Task<Guild> getDBForGuild(DiscordGuild guild) {
        using (var db = new GuildDB()) {
            var guildDB = await db.Guilds.FindAsync(guild.Id);
            if (guildDB != null) {
                // guild found
                await Console.Out.WriteLineAsync($"Guild ID {guild.Id} found in DB");
            }
            else {
                var newGuild = await db.Guilds.AddAsync(new Guild {
                    Id = guild.Id
                });
                await db.SaveChangesAsync();
                await Console.Out.WriteLineAsync($"Created new guild with ID {guild.Id} on DB");
                return newGuild.Entity;
            }

            return guildDB;
        }
    }

    /// <summary>
    /// Loads tracks from specified URL.
    /// </summary>
    /// <param name="uri">URL to load tracks from.</param>
    /// <returns>Loaded tracks.</returns>
    public Task<LavalinkLoadResult> GetTracksAsync(Uri uri)
        => node.Rest.GetTracksAsync(uri);

    public Task<LavalinkLoadResult> GetTracksAsync(string search)
        => node.Rest.GetTracksAsync(search);

    private async Task Lavalink_TrackExceptionThrown(LavalinkGuildConnection con, TrackExceptionEventArgs e) {
        if (e.Player?.Guild is null)
            return;

        if (!MusicData.TryGetValue(e.Player.Guild.Id, out var gmd))
            return;

        await gmd.CommandChannel.SendMessageAsync(
            $"{DiscordEmoji.FromName(client, ":pinkpill:")} A problem occured while playing {Formatter.Bold(Formatter.Sanitize(e.Track.Title))} by {Formatter.Bold(Formatter.Sanitize(e.Track.Author))}:\n{e.Error}");
    }
}

public class GuildDB : DbContext {
    public string DBPath { get; }
    public DbSet<Guild> Guilds { get; set; }

    public GuildDB() {
        var folder = Environment.CurrentDirectory;
        Console.Out.WriteLine($"Current directory is {folder} ");
        //if "bin" is present, remove all the path starting from "bin" word
        if (folder.Contains("bin")) {
            int index = folder.IndexOf("bin", StringComparison.Ordinal);
            folder = folder[..index];
        }

        DBPath = Path.Join(folder, "bot.db");
    }

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DBPath}");
}

public record Guild {
    public ulong Id { get; set; }
}