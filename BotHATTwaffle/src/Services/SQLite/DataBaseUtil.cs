using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using BotHATTwaffle.Models;
using Discord.Commands;
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

        public static void AddMute(SocketGuildUser user, int duration, SocketCommandContext context, string reason, DateTimeOffset dateTimeOffset)
        {
            using (var dbContext = new DataBaseContext())
            {
                dbContext.Mutes.Add(new Mute()
                {
                    snowflake = unchecked((long)user.Id),
                    username = $"{user}",
                    mute_reason = reason,
                    mute_duration = duration,
                    muted_by = $"{context.User}",
                    commandTime = dateTimeOffset
                });

                dbContext.SaveChanges();
            }
        }

        public static bool AddActiveMute(SocketGuildUser user, int duration, SocketCommandContext context, string reason, DateTimeOffset dateTimeOffset)
        {
            using (var dbContext = new DataBaseContext())
            {
                try {
                    dbContext.ActiveMutes.Add(new ActiveMute
                    {
                        snowflake = unchecked((long)user.Id),
                        username = $"{user}",
                        mute_reason = reason,
                        mute_duration = duration,
                        muted_by = $"{context.User}",
                        inMuteTimeOffset = dateTimeOffset
                    });

                    dbContext.SaveChanges();
                    return true;
                }
                catch (DbUpdateException)
                {
                    //Can't add cause an entry already exists
                    return false;
                }
            }
        }

        public static bool RemoveActiveMute(ulong userID)
        {
            using (var dbContext = new DataBaseContext())
            {
                try
                {
                    var mute = dbContext.ActiveMutes.Single(m => m.snowflake == (long)userID);

                    dbContext.ActiveMutes.Remove(mute);

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

        public static List<ActiveMute> GetActiveMutes()
        {
            using (var dbContext = new DataBaseContext())
            {
                return dbContext.ActiveMutes.ToList();
            }
        }

        /// <summary>
        /// Retrieves all mute records for a user.
        /// </summary>
        /// <param name="userId">The ID of the user for which to retrieve mutes.</param>
        /// <returns>A collection of the retrieved mute records.</returns>
        public static async Task<Mute[]> GetMutesAsync(ulong userId)
        {
            using (var dbContext = new DataBaseContext())
            {
                return await dbContext.Mutes.Where(m => m.snowflake.Equals(unchecked((long)userId))).ToArrayAsync();
            }
        }

        /// <summary>
        /// Retrieves a given quantity of the most recent mute records for a user.
        /// </summary>
        /// <param name="userId">The ID of the user for which to retrieve mutes.</param>
        /// <param name="quantity">The amount of recent records to retrieve.</param>
        /// <returns>A collection of the retrieved mute records in descending chronological order.</returns>
        public static async Task<Mute[]> GetMutesAsync(ulong userId, int quantity)
        {
            using (var dbContext = new DataBaseContext())
            {
                return await dbContext.Mutes.Where(m => m.snowflake.Equals(unchecked((long)userId)))
                    .OrderByDescending(m => m.date)
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
