﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using BotHATTwaffle.Models;
using Discord.WebSocket;

using Microsoft.EntityFrameworkCore;

namespace BotHATTwaffle
{
    class DataBaseUtil
    {
        public static void AddCommand(ulong snowflake, string username, string command, string fullmessage, DateTimeOffset dateTimeOffset)
        {
            using (var dbContext = new DataBaseContext())
            {
                dbContext.CommandUsage.Add(new CommandUse()
                {
                    snowflake = unchecked((long)snowflake),
                    username = username,
                    command = command,
                    fullmessage = fullmessage,
                    commandTime = dateTimeOffset
                });

                dbContext.SaveChanges();
            }
        }

        /// <summary>
        /// Retrieves a user's command usage records.
        /// </summary>
        /// <param name="userId">The ID of the user for which to retrieve records.</param>
        /// <returns>The retrieved records.</returns>
        public static async Task<CommandUse[]> GetCommands(ulong userId)
        {
            using (var dbContext = new DataBaseContext())
            {
                return await dbContext.CommandUsage.Where(r => r.snowflake.Equals((long)userId)).ToArrayAsync();
            }
        }

        public static void AddShitpost(ulong snowflake, string username, string shitpost, string fullmessage, DateTimeOffset dateTimeOffset)
        {
            using (var dbContext = new DataBaseContext())
            {
                dbContext.Shitposts.Add(new Shitpost()
                {
                    snowflake = unchecked((long)snowflake),
                    username = username,
                    shitpost = shitpost,
                    fullmessage = fullmessage,
                    commandTime = dateTimeOffset
                });

                dbContext.SaveChanges();
            }
        }

        /// <summary>
        /// Retrieves a user's shitpost log records.
        /// </summary>
        /// <param name="userId">The ID of the user for which to retrieve records.</param>
        /// <returns>The retrieved records.</returns>
        public static async Task<Shitpost[]> GetShitposts(ulong userId)
        {
            using (var dbContext = new DataBaseContext())
            {
                return await dbContext.Shitposts.Where(r => r.snowflake.Equals((long)userId)).ToArrayAsync();
            }
        }

        public static void AddKeyValue(string key, string value)
        {
            using (var dbContext = new DataBaseContext())
            {
                dbContext.KeyVaules.Add(new Key_Value()
                {
                    key = key,
                    value = value
                });

                dbContext.SaveChanges();
            }
        }

        public static Key_Value GetKeyValue(string requestedKey)
        {

            using (var dbContext = new DataBaseContext())
            {
                try
                {
                    return dbContext.KeyVaules.Single(s => s.key.Equals(requestedKey));
                }
                catch (InvalidOperationException)
                {
                    //If we got more than one, or none - return null.
                    return null;
                }

            }
        }

        public static void DeleteKeyValue(Key_Value kv)
        {
            using (var dbContext = new DataBaseContext())
            {
                dbContext.KeyVaules.Remove(kv);
                dbContext.SaveChanges();
            }
        }

        public static void AddSearchInformation()
        {
            //TODO: Actually code
        }

        public static List<SearchDataResult> GetSearchInformation(string[] search, string series)
        {
            List<SearchDataResult> found = new List<SearchDataResult>();
            List<SearchDataTag> temp = new List<SearchDataTag>();
            using (var dbContext = new DataBaseContext())
            {
                if(series.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var s in search)
                    {
                        temp.AddRange(
                            dbContext.SearchDataTags.Include(r => r.VirtualSearchDataResult)
                                .Where(a => a.tag.Equals(s.ToLower()))
                                .ToList());
                    }
                }
                else
                {
                    foreach (var s in search)
                    {
                        temp.AddRange(
                            dbContext.SearchDataTags.Include(r => r.VirtualSearchDataResult)
                                .Where(a => a.tag.Equals(s.ToLower()) && a.series.Equals(series))
                                .ToList());
                    }
                }

                //Remove doups if any, and convert to result.
                temp.Distinct().ToList().ForEach(x => found.Add(x.VirtualSearchDataResult));

                return found;
            }
        }

        public static void AddServer(Server server)
        {
            using (var dbContext = new DataBaseContext())
            {
                dbContext.Servers.Add(server);

                dbContext.SaveChanges();

                //TODO: Handle exception for if unique key exists already
            }
        }

        public static bool RemoveServer(Server server)
        {
            using (var dbContext = new DataBaseContext())
            {
                try
                {
                    dbContext.Servers.Remove(server);

                    dbContext.SaveChanges();

                    //Success
                    return true;
                }
                catch (InvalidOperationException)
                {
                    //Could not find server
                    return false;
                }
            }
        }

        public static Server GetServer(string serverStr)
        {
            using (var dbContext = new DataBaseContext())
            {
                try
                {
                    return dbContext.Servers.Single(s => s.name.Equals(serverStr));
                }
                catch (InvalidOperationException)
                {
                    //If we got more than one, or none - return null.
                    return null;
                }
            }
        }

        public static List<Server> GetAllServer()
        {
            using (var dbContext = new DataBaseContext())
            {
                return dbContext.Servers.ToList();
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
        /// <returns><c>true</c> if the mute was successfully added; <c>false</c> otherwise.</returns>
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
        /// <returns><c>true</c> if the mute was successfully added; <c>false</c> otherwise.</returns>
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
        /// <returns><c>true</c> if the mute was successfully expired; <c>false</c> otherwise.</returns>
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
        /// Retrieves all non-expired mute records.
        /// </summary>
        /// <returns>The retrieved records.</returns>
        public static async Task<Mute[]> GetActiveMutesAsync()
        {
            using (var dbContext = new DataBaseContext())
            {
                return await dbContext.Mutes.Where(m => !m.Expired).ToArrayAsync();
            }
        }

        /// <summary>
        /// Retrieves a user's non-expired mute record.
        /// </summary>
        /// <param name="userId">The ID of the user for which to retrieve the mute.</param>
        /// <returns>The retrieved record, or <c>null</c> if no record was found.</returns>
        public static async Task<Mute> GetActiveMuteAsync(ulong userId)
        {
            using (var dbContext = new DataBaseContext())
            {
                return await dbContext.Mutes.Where(m => m.UserId == userId && !m.Expired).SingleOrDefaultAsync();
            }
        }

        /// <summary>
        /// Retrieves all mute records.
        /// </summary>
        /// <returns>The retrieved records.</returns>
        public static async Task<Mute[]> GetMutesAsync()
        {
            using (var dbContext = new DataBaseContext())
            {
                return await dbContext.Mutes.ToArrayAsync();
            }
        }

        /// <summary>
        /// Retrieves all mute records for a user.
        /// </summary>
        /// <param name="userId">The ID of the user for which to retrieve records.</param>
        /// <returns>The retrieved records.</returns>
        public static async Task<Mute[]> GetMutesAsync(ulong userId)
        {
            using (var dbContext = new DataBaseContext())
            {
                return await dbContext.Mutes.Where(m => m.UserId == userId).ToArrayAsync();
            }
        }

        /// <summary>
        /// Retrieves a given quantity of the most recent mute records for a user.
        /// </summary>
        /// <param name="userId">The ID of the user for which to retrieve records.</param>
        /// <param name="quantity">The amount of recent records to retrieve.</param>
        /// <returns>The retrieved records in descending chronological order.</returns>
        public static async Task<Mute[]> GetMutesAsync(ulong userId, int quantity)
        {
            using (var dbContext = new DataBaseContext())
            {
                return await dbContext.Mutes.Where(m => m.UserId == userId)
                    .OrderByDescending(m => m.UnixTimeSeconds)
                    .Take(quantity)
                    // .OrderBy(m => m.date)
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