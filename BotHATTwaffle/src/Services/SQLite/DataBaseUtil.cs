using System;
using System.Collections.Generic;
using System.Linq;
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

		public static void AddServer(string name, string description, string rconPassword,
		string ftpPath, string ftpUsername, string ftpPassword, string ftpType)
		{
			using (var dbContext = new DataBaseContext())
			{
				dbContext.Servers.Add(new Server()
				{
					name = name,
					description = description,
					rcon_password = rconPassword,
					ftp_path = ftpPath,
					ftp_username = ftpUsername,
					ftp_password = ftpPassword,
					ftp_type = ftpType
				});

				dbContext.SaveChanges();
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

		public static List<Mute> GetMutes(SocketGuildUser user)
		{
			using (var dbContext = new DataBaseContext())
			{
				return dbContext.Mutes.Where(m => m.snowflake.Equals(unchecked((long)user.Id))).ToList();
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
