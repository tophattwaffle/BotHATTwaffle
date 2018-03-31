using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using BotHATTwaffle.Models;

using Discord.Commands;
using Discord.WebSocket;

using Microsoft.EntityFrameworkCore;

namespace BotHATTwaffle
{
    /// <summary>
    /// Contains functions which encapsulate queries on the database.
    /// </summary>
    class DataBaseUtil
    {
        /// <summary>
        /// Logs the use of a command.
        /// </summary>
        /// <param name="command">The name of the invoked command.</param>
        /// <param name="context">The context in which the command was invoked.</param>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        public static async Task AddCommandAsync(string command, SocketCommandContext context) =>
            await AddCommandAsync(context.User.Id, context.User.ToString(), command, context.Message.Content);

        /// <summary>
        /// Logs the use of a command.
        /// </summary>
        /// <param name="invokerId">The invoking user's ID.</param>
        /// <param name="invokerName">The invoking user's name.</param>
        /// <param name="command">The name of the invoked command.</param>
        /// <param name="msgContent">The contents of the message in which the command was invoked.</param>
        /// <param name="timestamp">When the command was invoked. Defaults to the current time.</param>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        public static async Task AddCommandAsync(
            ulong invokerId,
            string invokerName,
            string command,
            string msgContent,
            DateTimeOffset? timestamp = null)
        {
            using (var dbContext = new DataBaseContext())
            {
                dbContext.CommandUsage.Add(new CommandUse()
                {
                    snowflake = unchecked((long)invokerId),
                    username = invokerName,
                    command = command,
                    fullmessage = msgContent,
                    commandTime = timestamp ?? DateTimeOffset.UtcNow
                });

                await dbContext.SaveChangesAsync(); // Should never fail due to constraint violations.
            }
        }

        /// <summary>
        /// Retrieves a user's command usage records.
        /// </summary>
        /// <param name="userId">The ID of the user for which to retrieve records.</param>
        /// <returns>The retrieved command usage records.</returns>
        public static async Task<CommandUse[]> GetCommandsAsync(ulong userId)
        {
            using (var dbContext = new DataBaseContext())
            {
                return await dbContext.CommandUsage.Where(r => r.snowflake.Equals((long)userId)).AsNoTracking().ToArrayAsync();
            }
        }

        /// <summary>
        /// Logs a shitpost being triggered.
        /// </summary>
        /// <param name="shitpost">The name of the triggered shitpost.</param>
        /// <param name="message">The message in which the shitpost was triggered.</param>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        public static async Task AddShitpostAsync(string shitpost, SocketMessage message) =>
            await AddShitpostAsync(message.Author.Id, message.Author.ToString(), shitpost, message.Content);

        /// <summary>
        /// Logs a shitpost being triggered.
        /// </summary>
        /// <param name="triggererId">The triggering user's ID.</param>
        /// <param name="triggererName">The triggering user's name.</param>
        /// <param name="shitpost">The name of the triggered shitpost.</param>
        /// <param name="msgContent">The contents of the message in which the shitpost was triggered.</param>
        /// <param name="timestamp">When the shitpost was triggered. Defaults to the current time.</param>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        public static async Task AddShitpostAsync(
            ulong triggererId,
            string triggererName,
            string shitpost,
            string msgContent,
            DateTimeOffset? timestamp = null)
        {
            using (var dbContext = new DataBaseContext())
            {
                dbContext.Shitposts.Add(
                    new Shitpost
                    {
                        snowflake = unchecked((long)triggererId),
                        username = triggererName,
                        shitpost = shitpost,
                        fullmessage = msgContent,
                        commandTime = timestamp ?? DateTimeOffset.UtcNow
                    });

                await dbContext.SaveChangesAsync(); // Should never fail due to constraint violations.
            }
        }

        /// <summary>
        /// Retrieves a user's shitpost log records.
        /// </summary>
        /// <param name="userId">The ID of the user for which to retrieve records.</param>
        /// <returns>The retrieved shitpost records.</returns>
        public static async Task<Shitpost[]> GetShitpostsAsync(ulong userId)
        {
            using (var dbContext = new DataBaseContext())
            {
                return await dbContext.Shitposts.Where(r => r.snowflake.Equals((long)userId)).AsNoTracking().ToArrayAsync();
            }
        }

        /// <summary>
        /// Adds a key-value pair.
        /// </summary>
        /// <param name="key">The key of the pair to add.</param>
        /// <param name="value">The value of the pair to add.</param>
        /// <returns>
        /// <c>true</c> if successfully added; <c>false</c> if the <paramref name="key"/> already exists.</returns>
        public static async Task<bool> AddKeyValueAsync(string key, string value)
        {
            using (var dbContext = new DataBaseContext())
            {
                dbContext.KeyVaules.Add(new Key_Value { key = key, value = value });

                try
                {
                    await dbContext.SaveChangesAsync();

                    return true;
                }
                catch (DbUpdateException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Retrieves a key-value pair.
        /// </summary>
        /// <param name="key">The key of the key-value pair to retrieve.</param>
        /// <returns>The retrieved key-value pair or <c>null</c> if the <paramref name="key"/> could not be found.</returns>
        public static async Task<Key_Value> GetKeyValueAsync(string key)
        {
            using (var dbContext = new DataBaseContext())
            {
                return await dbContext.KeyVaules.AsNoTracking().SingleOrDefaultAsync(s => s.key.Equals(key));
            }
        }

        /// <summary>
        /// Removes a key-value pair.
        /// </summary>
        /// <param name="kv">The key-value pair to remove.</param>
        /// <returns><c>true</c> if successfully removed; <c>false</c> if the pair could not be found.</returns>
        public static async Task<bool> DeleteKeyValueAsync(Key_Value kv)
        {
            using (var dbContext = new DataBaseContext())
            {
                dbContext.KeyVaules.Remove(kv);

                try
                {
                    await dbContext.SaveChangesAsync();

                    return true;
                }
                catch (DbUpdateException)
                {
                    return false;
                }
            }
        }

        public static async Task<bool> AddSearchInformationAsync() => throw new NotImplementedException();

        /// <summary>
        /// Retrieves tutorials by their <paramref name="tags"/> and <paramref name="series"/>.
        /// </summary>
        /// <param name="tags">The tags for which to search.</param>
        /// <param name="series">The series for which to search.</param>
        /// <returns>The retrieved tutorials.</returns>
        public static async Task<ImmutableArray<SearchDataResult>> GetTutorialsAsync(string[] tags, string series)
        {
            var temp = new List<SearchDataTag>();

            using (var dbContext = new DataBaseContext())
            {
                if (series.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (string s in tags)
                    {
                        temp.AddRange(
                            await dbContext.SearchDataTags.Include(r => r.VirtualSearchDataResult)
                                .Where(a => a.tag.Equals(s.ToLower()))
                                .AsNoTracking()
                                .ToArrayAsync());
                    }
                }
                else
                {
                    foreach (string s in tags)
                    {
                        temp.AddRange(
                            await dbContext.SearchDataTags.Include(r => r.VirtualSearchDataResult)
                                .Where(a => a.tag.Equals(s.ToLower()) && a.series.Equals(series))
                                .AsNoTracking()
                                .ToArrayAsync());
                    }
                }

                // Remove any duplicates and convert to a result.
                return temp.Distinct().Select(t => t.VirtualSearchDataResult).ToImmutableArray();
            }
        }

        /// <summary>
        /// Adds a <see cref="server"/>.
        /// </summary>
        /// <param name="server">The server to add.</param>
        /// <returns><c>true</c> if successfully added; <c>false</c> if a server with the same name already exists.</returns>
        public static async Task<bool> AddServerAsync(Server server)
        {
            using (var dbContext = new DataBaseContext())
            {
                dbContext.Servers.Add(server);

                try
                {
                    await dbContext.SaveChangesAsync();

                    return true;
                }
                catch (DbUpdateException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Removes a <paramref name="server"/>.
        /// </summary>
        /// <param name="server">The server to remove.</param>
        /// <returns>
        /// <c>true</c> if successfully removed; <c>false</c> if the <paramref name="server"/> could not be found.
        /// </returns>
        public static async Task<bool> RemoveServerAsync(Server server)
        {
            using (var dbContext = new DataBaseContext())
            {
                dbContext.Servers.Remove(server);

                try
                {
                    await dbContext.SaveChangesAsync();

                    return true;
                }
                catch (DbUpdateException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Retrieves a server which has the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the server to retrieve.</param>
        /// <returns>The retrieved server or <c>null</c> if the server could not be found.</returns>
        public static async Task<Server> GetServerAsync(string name)
        {
            using (var dbContext = new DataBaseContext())
            {
                return await dbContext.Servers.AsNoTracking().SingleOrDefaultAsync(s => s.name.Equals(name));
            }
        }

        /// <summary>
        /// Retrieves all playtesting servers.
        /// </summary>
        /// <returns>The retrieved servers.</returns>
        public static async Task<Server[]> GetServersAsync()
        {
            using (var dbContext = new DataBaseContext())
            {
                return await dbContext.Servers.AsNoTracking().ToArrayAsync();
            }
        }

        /// <summary>
        /// Adds a mute record.
        /// </summary>
        /// <param name="user">The user to mute.</param>
        /// <param name="muter">The user which invoked the mute operation.</param>
        /// <param name="timestamp">When the mute was issued.</param>
        /// <param name="duration">The duration, in minutes, of the mute.</param>
        /// <param name="reason">The reason for the mute.</param>
        /// <returns><c>true</c> if the mute was successfully added; <c>false</c> if the user is already muted.</returns>
        public static async Task<bool> AddMuteAsync(
            SocketGuildUser user,
            SocketGuildUser muter,
            DateTimeOffset timestamp,
            long? duration = null,
            string reason = null)
        {
            var mute = new Mute
            {
                UserId = user.Id,
                Username = user.ToString(),
                Reason = reason,
                Duration = duration,
                MuterName = muter.ToString(),
                Timestamp = timestamp
            };

            return await AddMuteAsync(mute);
        }

        /// <summary>
        /// Adds a mute record.
        /// </summary>
        /// <param name="mute">The mute to add.</param>
        /// <returns><c>true</c> if the mute was successfully added; <c>false</c> if the user is already muted.</returns>
        public static async Task<bool> AddMuteAsync(Mute mute)
        {
            using (var dbContext = new DataBaseContext())
            {
                dbContext.Mutes.Add(mute);

                try
                {
                    await dbContext.SaveChangesAsync();

                    return true;
                }
                catch (DbUpdateException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Sets a user's active mute to expired.
        /// </summary>
        /// <param name="userId">The user for which to expire a mute.</param>
        /// <returns><c>true</c> if the mute was successfully set as expired; <c>false</c> if the user isn't muted.</returns>
        public static async Task<bool> ExpireMuteAsync(ulong userId)
        {
            using (var dbContext = new DataBaseContext())
            {
                Mute mute = await GetActiveMuteAsync(userId);

                if (mute == null)
                    return false;

                mute.Expired = true;
                dbContext.Mutes.Update(mute);
                await dbContext.SaveChangesAsync();

                return true;
            }
        }

        /// <summary>
        /// Retrieves all non-expired mute records sorted in descending chronological order.
        /// </summary>
        /// <returns>The retrieved mute records in descending chronological order.</returns>
        public static async Task<Mute[]> GetActiveMutesAsync()
        {
            using (var dbContext = new DataBaseContext())
            {
                return await dbContext.Mutes.Where(m => !m.Expired)
                    .OrderByDescending(m => m.UnixTimeSeconds)
                    .AsNoTracking()
                    .ToArrayAsync();
            }
        }

        /// <summary>
        /// Retrieves a user's non-expired mute record.
        /// </summary>
        /// <param name="userId">The ID of the user for which to retrieve the mute.</param>
        /// <returns>
        /// The retrieved mute record or <c>null</c> if the user isn't muted.
        /// </returns>
        public static async Task<Mute> GetActiveMuteAsync(ulong userId)
        {
            using (var dbContext = new DataBaseContext())
            {
                return await dbContext.Mutes.Where(m => m.UserId == userId && !m.Expired).AsNoTracking().SingleOrDefaultAsync();
            }
        }

        /// <summary>
        /// Retrieves all mute records sorted in descending chronological order.
        /// </summary>
        /// <returns>The retrieved mute records in descending chronological order.</returns>
        public static async Task<Mute[]> GetMutesAsync()
        {
            using (var dbContext = new DataBaseContext())
            {
                return await dbContext.Mutes.OrderByDescending(m => m.UnixTimeSeconds).AsNoTracking().ToArrayAsync();
            }
        }

        /// <summary>
        /// Retrieves all mute records for a user sorted in descending chronological order.
        /// </summary>
        /// <param name="userId">The ID of the user for which to retrieve records.</param>
        /// <returns>The retrieved mute records in descending chronological order.</returns>
        public static async Task<Mute[]> GetMutesAsync(ulong userId)
        {
            using (var dbContext = new DataBaseContext())
            {
                return await dbContext.Mutes.Where(m => m.UserId == userId)
                    .OrderByDescending(m => m.UnixTimeSeconds)
                    .AsNoTracking()
                    .ToArrayAsync();
            }
        }

        /// <summary>
        /// Retrieves a given quantity of the most recent mute records for a user sorted in descending chronological order.
        /// </summary>
        /// <param name="userId">The ID of the user for which to retrieve records.</param>
        /// <param name="quantity">The amount of recent records to retrieve.</param>
        /// <returns>The retrieved mute records in descending chronological order.</returns>
        public static async Task<Mute[]> GetMutesAsync(ulong userId, int quantity)
        {
            using (var dbContext = new DataBaseContext())
            {
                return await dbContext.Mutes.Where(m => m.UserId == userId)
                    .OrderByDescending(m => m.UnixTimeSeconds)
                    .Take(quantity)
                    .AsNoTracking()
                    .ToArrayAsync();
            }
        }
    }
}

/* Cheatsheet for queries
//ADDING DATA
dataContext.Servers.Add(new Server() { name = "test", address = "asd.com"});
dataContext.Database.ExecuteSqlCommand(@"INSERT INTO Servers ('name','address') VALUES ('testing','asd.com')");
dataContext.SaveChanges(); //Data not added until you save

//GETTING DATA, SINGLE
var server = dataContext.Servers.FromSql(@"SELECT * FROM Servers where name = 'amd'").FirstOrDefault();
var server2 = dataContext.Servers.Single(s => s.name == "amd");

//GETTING DATA, LISTS
var serverCom = dataContext.Servers.FromSql(@"SELECT * FROM Servers where address = '%.com'").ToList();
var serverCom2 = dataContext.Servers.Where(s => s.address.Contains(".com"));
var serverCom3 = dataContext.Servers.ToList(); //ENTIRE TABLE
*/

//var item = dataContext.SearchData_Results.Include(i => i.tags.Any(t => t.tag == "light")).Single();

//Cross select based on the a.tag conditions
//var test = dataContext.SearchData_Tags.Include(r => r.SearchDataResult).Where(a => a.tag.Equals("light")).ToList();

/*
foreach (var t in test)
{
    Console.WriteLine($"{t.tag}\n{t.SearchDataResult.url}\n{t.SearchDataResult.name}\n");
}
*/

/*
string serverCode = "ena";
Server server = dataContext.Servers.Single(s => s.name.Equals(serverCode));

Console.WriteLine(server.ToString());
*/
