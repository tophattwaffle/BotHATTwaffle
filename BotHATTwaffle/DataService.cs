using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.IO;
using BotHATTwaffle.Modules.Json;
using HtmlAgilityPack;
using System.Threading.Tasks;
using CoreRCON;
using System.Net;
using FluentFTP;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using Discord;
using Discord.WebSocket;
using System.Web;

namespace BotHATTwaffle
{
    public class DataServices
    {
        public Dictionary<string, string> config;
        
        JObject searchData;
        JObject serverData;
        JsonRoot root;
        List<JsonSeries> series;
        List<JsonServer> servers;
        Random _random;
        private string demoPath;

        //Channels and Role vars
        string logChannelStr;
        string playTesterRoleStr;
        string announcementChannelStr;
        string testingChannelStr;
        string modRoleStr;
        string mutedRoleStr;
        string rconRoleStr;
        string ActiveRoleStr;
        public SocketTextChannel logChannel { get; set; }
        public SocketTextChannel announcementChannel  { get; set; }
        public SocketTextChannel testingChannel  { get; set; }
        public SocketRole playTesterRole { get; set; }
        public SocketRole MuteRole { get; set; }
        public SocketRole RconRole { get; set; }
        public SocketRole ModRole { get; set; }
        public SocketRole ActiveRole { get; set; }

        //Misc setting vars
        public string[] pakRatEavesDrop;
        public string[] howToPackEavesDrop;
        public string[] carveEavesDrop;
        public string[] propperEavesDrop;
        public string[] vbEavesDrop;
        public string[] yorkEavesDrop;
        public string[] agreeEavesDrop;
        public string[] agreeStrings;
        public string[] roleMeWhiteList;
        public string catFactPath;
        public string penguinFactPath;
        public string alertUser;

        //TimerService Vars
        public int startDelay = 10;
        public int updateInterval = 60;
        public string[] playingStrings;

        //Moderation Vars
        public string casualConfig;
        public string compConfig;
        public string postConfig;

        //LevelTesting Vars
        public string[] publicCommandWhiteList;
        public int calUpdateTicks = 2;

        public DataServices(Random random)
        {
            config = ReadSettings(); //Needed when the data is first DI'd
            _random = random;
        }

        public void ReadData()
        {
            config = ReadSettings();
            RoleChannelAssignments();
            VariableAssignment();
            string searchDataPath = "searchData.json";
            searchData = JObject.Parse(File.ReadAllText(searchDataPath));
            root = searchData.ToObject<JsonRoot>();
            series = root.series;

            string serverDataPath = "servers.json";
            serverData = JObject.Parse(File.ReadAllText(serverDataPath));
            root = serverData.ToObject<JsonRoot>();
            servers = root.servers;

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("SETTINGS HAVE BEEN LOADED\n");
            Console.ResetColor();
        }

        private Dictionary<string, string> ReadSettings()
        {
            string path = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            Dictionary<string, string> mainConfig;
            string configPath = "settings.ini";
            if (File.Exists(configPath))
            {
                mainConfig = File.ReadAllLines(configPath).ToDictionary(line => line.Split('=')[0].Trim(), line => line.Split('=')[1].Trim());
            }
            else
            {
                // Config doesn't exist, so we'll make it
                mainConfig = new Dictionary<string, string>();

                //Get INT
                //if (config.ContainsKey("clockDelay"))
                //int.TryParse(config["clockDelay"], out clockDelay);

                //Get String
                //if (config.ContainsKey("botToken"))
                //botToken = (config["botToken"]);


                //Get Char
                //if (config.ContainsKey("prefixChar"))
                //prefixChar = config["prefixChar"][0];
            }

            #region Add existing settings at their default
            #region General or global
            mainConfig.AddKeyIfMissing("botToken", "NEEDS_TO_BE_REPLACED");
            mainConfig.AddKeyIfMissing("startDelay", "10");
            mainConfig.AddKeyIfMissing("updateInterval", "60");
            mainConfig.AddKeyIfMissing("calUpdateTicks", "1");
            mainConfig.AddKeyIfMissing("logChannel", "bothattwaffle_logs");
            mainConfig.AddKeyIfMissing("announcementChannel", "announcements");
            mainConfig.AddKeyIfMissing("playingStringsCSV", "Eating Waffles,Not working on Titan,The year is 20XX,Hopefully not crashing,>help,>upcoming");
            mainConfig.AddKeyIfMissing("agreeUserCSV", "TopHATTwaffle,Phoby,thewhaleman,maxgiddens,CSGO John Madden,Wazanator,TanookiSuit3,JSadones,Lykrast,maxgiddens,Zelz Storm");
            mainConfig.AddKeyIfMissing("alertUser", "[DISCORD NAME OF USER WITH #]");
            #endregion

            #region Playtesting vars
            mainConfig.AddKeyIfMissing("testCalID", "Replace My Buddy");
            mainConfig.AddKeyIfMissing("playTesterRole", "Playtester");
            mainConfig.AddKeyIfMissing("activeMemberRole", "Active Member");
            mainConfig.AddKeyIfMissing("testingChannel", "csgo_level_testing");
            mainConfig.AddKeyIfMissing("demoPath", $"X:\\Playtesting Demos");
            mainConfig.AddKeyIfMissing("casualConfig", $"thw");
            mainConfig.AddKeyIfMissing("compConfig", $"thw");
            mainConfig.AddKeyIfMissing("postConfig", $"postame");
            #endregion

            #region Eavesdropping vars
            mainConfig.AddKeyIfMissing("pakRatEavesDropCSV", "use pakrat,download pakrat,get pakrat,use packrat");
            mainConfig.AddKeyIfMissing("howToPackEavesDropCSV", "how do i pack,how can i pack,how to pack,how to use vide,help me pack");
            mainConfig.AddKeyIfMissing("carveEavesDropCSV", "carve");
            mainConfig.AddKeyIfMissing("propperEavesDropCSV", "use propper,download propper,get propper,configure propper,setup propper");
            mainConfig.AddKeyIfMissing("vbEavesDropCSV", "velocity brawl,velocitybrawl,velocity ballsack");
            mainConfig.AddKeyIfMissing("yorkCSV", "de_york,de york");
            #endregion

            #region Command Dependent
            mainConfig.AddKeyIfMissing("roleMeWhiteListCSV", "Programmer,Level_Designer,3D_Modeler,Texture_Artist,Blender,Maya,3dsmax");
            mainConfig.AddKeyIfMissing("moderatorRoleName", "Moderators");
            mainConfig.AddKeyIfMissing("mutedRoleName", "Muted");
            mainConfig.AddKeyIfMissing("rconRoleName", "RconAccess");
            mainConfig.AddKeyIfMissing("publicCommandWhiteListCSV", "[CONFIGME]");
            #endregion

            #region  Shitpost vars
            mainConfig.AddKeyIfMissing("catFactPath", $"X:\\Scripts\\catfacts.txt");
            mainConfig.AddKeyIfMissing("catFactPath", $"X:\\Scripts\\penguinfacts.txt");
            #endregion

            #endregion

            // Save new config file
            File.WriteAllLines(configPath, mainConfig.Select(kvp => $"{kvp.Key} = {kvp.Value}").ToArray());

            return mainConfig;
        }

        private void VariableAssignment()
        {
            if (config.ContainsKey("demoPath"))
                demoPath = (config["demoPath"]);
            if (config.ContainsKey("pakRatEavesDropCSV"))
                pakRatEavesDrop = (config["pakRatEavesDropCSV"]).Split(',');
            if (config.ContainsKey("howToPackEavesDropCSV"))
                howToPackEavesDrop = (config["howToPackEavesDropCSV"]).Split(',');
            if (config.ContainsKey("carveEavesDropCSV"))
                carveEavesDrop = (config["carveEavesDropCSV"]).Split(',');
            if (config.ContainsKey("propperEavesDropCSV"))
                propperEavesDrop = (config["propperEavesDropCSV"]).Split(',');
            if (config.ContainsKey("vbEavesDropCSV"))
                vbEavesDrop = (config["vbEavesDropCSV"]).Split(',');
            if (config.ContainsKey("yorkCSV"))
                yorkEavesDrop = (config["yorkCSV"]).Split(',');
            if (config.ContainsKey("agreeUserCSV"))
                agreeEavesDrop = (config["agreeUserCSV"]).Split(',');
            if (config.ContainsKey("roleMeWhiteListCSV"))
                roleMeWhiteList = (config["roleMeWhiteListCSV"]).Split(',');
            if ((config.ContainsKey("startDelay") && !int.TryParse(config["startDelay"], out startDelay)))
            {
                Console.WriteLine($"Key \"startDelay\" not found or valid. Using default {startDelay}.");
            }
            if ((config.ContainsKey("updateInterval") && !int.TryParse(config["updateInterval"], out updateInterval)))
            {
                Console.WriteLine($"Key \"updateInterval\" not found or valid. Using default {updateInterval}.");
            }
            if (config.ContainsKey("casualConfig"))
                casualConfig = (config["casualConfig"]);
            if (config.ContainsKey("compConfig"))
                compConfig = (config["compConfig"]);
            if (config.ContainsKey("postConfig"))
                postConfig = (config["postConfig"]);
            if (config.ContainsKey("publicCommandWhiteListCSV"))
                publicCommandWhiteList = (config["publicCommandWhiteListCSV"]).Split(',');
            if (config.ContainsKey("catFactPath"))
                catFactPath = (config["catFactPath"]);
            if (config.ContainsKey("penguinFactPath"))
                penguinFactPath = (config["penguinFactPath"]);
            if ((config.ContainsKey("calUpdateTicks") && !int.TryParse(config["calUpdateTicks"], out calUpdateTicks)))
            {
                Console.WriteLine($"Key \"calUpdateTicks\" not found or valid. Using default {calUpdateTicks}.");
            }
            if (config.ContainsKey("playingStringsCSV"))
                playingStrings = (config["playingStringsCSV"]).Split(',');
            if (config.ContainsKey("alertUser"))
                alertUser = (config["alertUser"]);
        }

        private void RoleChannelAssignments()
        {
            if (config.ContainsKey("announcementChannel"))
                announcementChannelStr = (config["announcementChannel"]);

            if (config.ContainsKey("logChannel"))
                logChannelStr = (config["logChannel"]);

            if (config.ContainsKey("testingChannel"))
                testingChannelStr = (config["testingChannel"]);

            if (config.ContainsKey("playTesterRole"))
                playTesterRoleStr = (config["playTesterRole"]);

            if (config.ContainsKey("moderatorRoleName"))
                modRoleStr = (config["moderatorRoleName"]);

            if (config.ContainsKey("mutedRoleName"))
                mutedRoleStr = (config["mutedRoleName"]);

            if (config.ContainsKey("rconRoleName"))
                rconRoleStr = (config["rconRoleName"]);

            if (config.ContainsKey("activeMemberRole"))
                ActiveRoleStr = (config["activeMemberRole"]);

            var arg = Program._client.Guilds.FirstOrDefault();

            Console.ForegroundColor = ConsoleColor.Green;
            //Iterate all channels
            foreach (SocketTextChannel s in arg.TextChannels)
            {
                if (s.Name == logChannelStr)
                {
                    logChannel = s;
                    Console.WriteLine($"\nLog Channel Found! Logging to: {s.Name}");
                }
                if (s.Name == announcementChannelStr)
                {
                    announcementChannel = s;
                    Console.WriteLine($"\nAnnouncement Channel Found! Announcing to: {s.Name}");
                }
                if (s.Name == testingChannelStr)
                {
                    testingChannel = s;
                    Console.WriteLine($"\nTesting Channel Found! Sending playtest alerts to: {s.Name}");

                }
            }

            foreach (SocketRole r in arg.Roles)
            {
                if (r.Name == playTesterRoleStr)
                {
                    playTesterRole = r;
                    Console.WriteLine($"\nPlaytester role found!: {r.Name}");
                }
                if (r.Name == modRoleStr)
                {

                    ModRole = r;
                    Console.WriteLine($"\nModerator role found!: {r.Name}");
                }
                if (r.Name == rconRoleStr)
                {
                    RconRole = r;
                    Console.WriteLine($"\nRCON role found!: {r.Name}");
                }
                if (r.Name == mutedRoleStr)
                {
                    MuteRole = r;
                    Console.WriteLine($"\nMuted role found!: {r.Name}");
                }
                if (r.Name == ActiveRoleStr)
                {
                    ActiveRole = r;
                    Console.WriteLine($"\nActive Memeber role found!: {r.Name}"); //That isn't a spelling mistake :kappa:
                }
            }
            Console.WriteLine();
            Console.ResetColor();
        }

        public Task ChannelLog(string message, Boolean mention = false)
        {
            string alert = null;
            if (mention)
            {
                var splitUser = alertUser.Split('#');
                alert = Program._client.GetUser(splitUser[0], splitUser[1]).Mention;
            }

            logChannel.SendMessageAsync($"{alert}```{DateTime.Now}\n{message}```");
            Console.WriteLine($"{DateTime.Now}: {message}\n");
            return Task.CompletedTask;
        }

        public Task ChannelLog(string title, string message, Boolean mention = false)
        {
            string alert = null;
            if (mention)
            {
                var splitUser = alertUser.Split('#');
                alert = Program._client.GetUser(splitUser[0], splitUser[1]).Mention;
            }

            logChannel.SendMessageAsync($"{alert}```{DateTime.Now}\n{title}\n{message}```");
            Console.WriteLine($"{DateTime.Now}: {title}\n{message}\n");
            return Task.CompletedTask;
        }

        public JsonServer GetServer(string serverStr)
        {
            return servers.Find(x => x.Name == serverStr.ToLower());
        }

        public Embed GetAllServers()
        {
            var authBuilder = new EmbedAuthorBuilder()
            {
                Name = $"Server List",
                IconUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
            };

            List<EmbedFieldBuilder> fieldBuilder = new List<EmbedFieldBuilder>();
            foreach (var s in servers)
            {
                fieldBuilder.Add(new EmbedFieldBuilder { Name = $"{s.Address}", Value = $"Prefix: `{s.Name}`\n{s.Description}", IsInline = false });
            }

            var builder = new EmbedBuilder()
            {
                Fields = fieldBuilder,
                Author = authBuilder,
                Color = new Color(243, 128, 72),

                Description = $""
            };

            return builder;
        }


        async public Task<string> RconCommand(string command, JsonServer server)
        {
            string reply = null;
            string botIP = new WebClient().DownloadString("http://icanhazip.com").Trim();
            IPHostEntry iPHostEntry = null;
            try
            {
                iPHostEntry = Dns.GetHostEntry(server.Address);
            }
            catch
            {
                return "HOST_NOT_FOUND";
            }

            var rcon = new RCON(IPAddress.Parse($"{iPHostEntry.AddressList[0]}"), 27015, server.Password,1000);
            
            reply = await rcon.SendCommandAsync(command);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"RCON COMMAND: {server.Address}\nCommand: {command}\n");
            Console.ResetColor();

#pragma warning disable CS4014 //If you re-set the rcon_password all RCON connections are closed.
            //By not awaiting this, we are able to set the rcon password back to the samve value closing the connection.
            //This will automatically timeout and dispose of the rcon connection when it tries to conncect again.
            rcon.SendCommandAsync($"rcon_password {server.Password}");
#pragma warning restore CS4014

            reply = reply.Replace($"{botIP}", "69.420.MLG.1337"); //Remove the Bot's public IP from the string.

            return reply;
        }

        public List<List<string>> Search(string searchSeries, string searchTerm, bool isPrivate)
        {
            List<JsonTutorial> foundTutorials = new List<JsonTutorial>();
            List<List<string>> listResults = new List<List<string>>();
            string[] searchTermArray = searchTerm.Split(' ');

            if (searchSeries.ToLower() == "faq" || searchSeries.ToLower() == "f" || searchSeries.ToLower() == "7")
                return SearchFAQ(searchTerm, isPrivate);

            if (searchTerm.ToLower() == "dump" || searchTerm.ToLower() == "all")
                return DumpSearch(searchSeries);

            //V2 0
            if (searchSeries.ToLower() == "v2series" || searchSeries.ToLower() == "v2" || searchSeries.ToLower() == "1" || searchSeries.ToLower() == "all")
            {
                foreach (string s in searchTermArray)
                {
                    foundTutorials.AddRange(series[0].tutorial.FindAll(x => x.tags.Contains(s)));
                }
            }
            //Bootcamp 1
            if (searchSeries.ToLower() == "csgobootcamp" || searchSeries.ToLower() == "bc" || searchSeries.ToLower() == "2" || searchSeries.ToLower() == "all")
            {
                foreach (string s in searchTermArray)
                {
                    foundTutorials.AddRange(series[1].tutorial.FindAll(x => x.tags.Contains(s)));
                }
            }
            //3dsmax 2
            if (searchSeries.ToLower() == "3dsmax" || searchSeries.ToLower() == "3ds" || searchSeries.ToLower() == "3" || searchSeries.ToLower() == "all")
            {
                foreach (string s in searchTermArray)
                {
                    foundTutorials.AddRange(series[2].tutorial.FindAll(x => x.tags.Contains(s)));
                }
            }
            //Writtentutorials 3
            if (searchSeries.ToLower() == "writtentutorials" || searchSeries.ToLower() == "written" || searchSeries.ToLower() == "4" || searchSeries.ToLower() == "all")
            {
                foreach (string s in searchTermArray)
                {
                    foundTutorials.AddRange(series[3].tutorial.FindAll(x => x.tags.Contains(s)));
                }
            }
            //legacy 5
            if (searchSeries.ToLower() == "legacyseries" || searchSeries.ToLower() == "v1" || searchSeries.ToLower() == "lg" || searchSeries.ToLower() == "5" || searchSeries.ToLower() == "all")
            {
                foreach (string s in searchTermArray)
                {
                    foundTutorials.AddRange(series[5].tutorial.FindAll(x => x.tags.Contains(s)));
                }
            }
            //troubleshooting 4
            if (searchSeries.ToLower() == "hammertroubleshooting" || searchSeries.ToLower() == "ht" || searchSeries.ToLower() == "6" || searchSeries.ToLower() == "misc" || searchSeries.ToLower() == "all")
            {
                foreach (string s in searchTermArray)
                {
                    foundTutorials.AddRange(series[4].tutorial.FindAll(x => x.tags.Contains(s)));
                }
            }

            //Remove douplicates from list.
            List<JsonTutorial> noDoups = foundTutorials.Distinct().ToList();

            foreach (var result in noDoups)
            {
                List<string> singleResult = new List<string>();

                //Limit to 3 FAQ resusults. Let's add another one with a direct link to the page. Only limit for non-DM
                if (listResults.Count >= 2 && searchSeries == "all" && !isPrivate)
                {
                    singleResult.Clear();
                    singleResult.Add(@"View All Tutorials");
                    singleResult.Add("https://www.tophattwaffle.com/tutorials/");
                    singleResult.Add(@"There are more results than I can display without flooding chat. [Consider viewing all tutorials](https://www.tophattwaffle.com/tutorials/), or do a search without `all`. If you DM me your search the results won't be limited.");
                    singleResult.Add(null);
                    listResults.Add(singleResult);
                    break;
                }

                HtmlWeb htmlWeb = new HtmlWeb();
                HtmlDocument htmlDocument = htmlWeb.Load(result.url);
                string title = null;
                if (!result.url.Contains("youtube"))
                {
                    title = (from x in htmlDocument.DocumentNode.Descendants()
                             where x.Name.ToLower() == "title"
                             select x.InnerText).FirstOrDefault();
                }
                else if (result.url.Contains("youtube"))//Is a youtube URL
                {
                    title = GetTitle(result.url);
                }

                string description = null;
                //Get atricle content, this is by ID. Only works for my site.
                if (result.url.ToLower().Contains("tophattwaffle"))
                {
                    description = htmlDocument.GetElementbyId("content-area").InnerText;
                }
                else if(result.url.ToLower().Contains("youtube"))
                {
                    description = result.url;
                }
                //Fix the bad characters that get pulled from the web page.
                if (description != null)
                    description = description.Replace(@"&#8211;", "-").Replace("\n", "").Replace(@"&#8220;", "\"").Replace(@"&#8221;", "\"").Replace(@"&#8217;", "'");

                title = title.Replace(@"&#8211;", "-").Replace("\n", "").Replace(" | TopHATTwaffle", "").Replace(@"&#8220;", "\"").Replace(@"&#8221;", "\"").Replace(@"&#8217;", "'");

                //Limit length if needed
                if (description != null && description.Length >= 250)
                {
                    description = description.Substring(0, 250) + "...";
                }
                List<string> imgs = null;
                //Get images on the page

                if (!result.url.ToLower().Contains("youtube"))
                { 
                imgs = (from x in htmlDocument.DocumentNode.Descendants()
                                     where x.Name.ToLower() == "img"
                                     select x.Attributes["src"].Value).ToList<String>();
                }

                //Set image to the first non-header image if it exists.
                string finalImg = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png";
                if (imgs != null && imgs.Count > 1)
                    finalImg = imgs[_random.Next(0, imgs.Count)];

                if (result.url.Contains("youtube"))
                {
                    finalImg = GetYouTubeImage(result.url);
                }

                singleResult.Add(title);
                singleResult.Add(result.url);
                singleResult.Add(description);
                singleResult.Add(finalImg);
                listResults.Add(singleResult);
            }
            return listResults;
        }

        //Youtube info
        public static string GetTitle(string url)
        {
            var api = $"http://youtube.com/get_video_info?video_id={GetArgs(url, "v", '?')}";
            return GetArgs(new WebClient().DownloadString(api), "title", '&');
        }

        private static string GetArgs(string args, string key, char query)
        {
            var iqs = args.IndexOf(query);
            return iqs == -1
                ? string.Empty
                : HttpUtility.ParseQueryString(iqs < args.Length - 1
                    ? args.Substring(iqs + 1) : string.Empty)[key];
        }

        public static string GetYouTubeImage(string videoUrl)
        {
            int mInd = videoUrl.IndexOf("/watch?v=");
            if (mInd != -1)
            {
                string strVideoCode = videoUrl.Substring(videoUrl.IndexOf("/watch?v=") + 9);
                return "https://img.youtube.com/vi/" + strVideoCode + "/hqdefault.jpg";
            }
            else
                return "";
        }

        public List<List<string>> DumpSearch(string searchSeries)
        {
            List<List<string>> listResults = new List<List<string>>();
            List<JsonTutorial> foundTutorials = new List<JsonTutorial>();

            //V2 0
            if (searchSeries.ToLower() == "v2series" || searchSeries.ToLower() == "v2" || searchSeries.ToLower() == "1" || searchSeries.ToLower() == "all")
            {
                foundTutorials.AddRange(series[0].tutorial);
            }
            //Bootcamp 1
            if (searchSeries.ToLower() == "csgobootcamp" || searchSeries.ToLower() == "bc" || searchSeries.ToLower() == "2" || searchSeries.ToLower() == "all")
            {
                foundTutorials.AddRange(series[1].tutorial);
            }
            //3dsmax 2
            if (searchSeries.ToLower() == "3dsmax" || searchSeries.ToLower() == "3ds" || searchSeries.ToLower() == "3" || searchSeries.ToLower() == "all")
            {
                foundTutorials.AddRange(series[2].tutorial);
            }
            //Writtentutorials 3
            if (searchSeries.ToLower() == "writtentutorials" || searchSeries.ToLower() == "written" || searchSeries.ToLower() == "4" || searchSeries.ToLower() == "all")
            {
                foundTutorials.AddRange(series[3].tutorial);
            }
            //legacy 5
            if (searchSeries.ToLower() == "legacyseries" || searchSeries.ToLower() == "v1" || searchSeries.ToLower() == "lg" || searchSeries.ToLower() == "5" || searchSeries.ToLower() == "all")
            { 
               foundTutorials.AddRange(series[5].tutorial);

            }
            //troubleshooting 4
            if (searchSeries.ToLower() == "hammertroubleshooting" || searchSeries.ToLower() == "ht" || searchSeries.ToLower() == "6" || searchSeries.ToLower() == "misc" || searchSeries.ToLower() == "all")
            {
                foundTutorials.AddRange(series[4].tutorial);
            }

            foreach (var result in foundTutorials)
            {
                List<string> singleResult = new List<string>();

                //Doing a web request times the bot out at times. Not worth it.
                /*
                HtmlWeb htmlWeb = new HtmlWeb();
                HtmlDocument htmlDocument = htmlWeb.Load(result.url);

                string title = (from x in htmlDocument.DocumentNode.Descendants()
                                where x.Name.ToLower() == "title"
                                select x.InnerText).FirstOrDefault();
                title = title.Replace(@"&#8211;", "-").Replace("\n", "").Replace(" | TopHATTwaffle", "").Replace(@"&#8220;", "\"").Replace(@"&#8221;", "\"").Replace(@"&#8217;", "'");
                */

                singleResult.Add(result.url.Replace("https://www.tophattwaffle.com/","").Replace("/",""));
                singleResult.Add(result.url);
                singleResult.Add(string.Join(", ", result.tags));
                listResults.Add(singleResult);
            }

            return listResults;
        }

        public List<List<string>> SearchFAQ(string searchTerm, bool isPrivate)
        {
            List<List<string>> listResults = new List<List<string>>();
            
            string faqurl = "https://www.tophattwaffle.com/wp-admin/admin-ajax.php?action=epkb-search-kb&epkb_kb_id=1&search_words=";
            try
            {
                HtmlWeb faqWeb = new HtmlWeb();
                HtmlDocument faqDocument = faqWeb.Load($"{faqurl}{searchTerm}");
                foreach (HtmlNode link in faqDocument.DocumentNode.SelectNodes("//a[@href]"))
                {
                    List<string> singleResult = new List<string>();

                    //Limit to 3 FAQ resusults. Let's add another one with a direct link to the page.  Only limit for non-DM.
                    if (listResults.Count >= 2 && !isPrivate)
                    {
                        singleResult.Clear();
                        singleResult.Add(@"I cannot display any more results!");
                        singleResult.Add("http://tophattwaffle.com/faq");
                        singleResult.Add(@"I found more results than I can display here. Consider going directly to the FAQ main page and searching from there. If you DM me your search results won't be limited.");
                        singleResult.Add(null);
                        listResults.Add(singleResult);
                        break;
                    }

                    //Setup the web request for this specific link found. Format it so we can get data about it.
                    string finalUrl = link.GetAttributeValue("href", string.Empty).Replace(@"\", "").Replace("\"", "");
                    HtmlWeb htmlWeb = new HtmlWeb();
                    HtmlDocument htmlDocument = htmlWeb.Load(finalUrl);

                    //Get page title.
                    string title = (from x in htmlDocument.DocumentNode.Descendants()
                                    where x.Name.ToLower() == "title"
                                    select x.InnerText).FirstOrDefault();

                    //Get atricle content, this is by ID. Only works for my site.
                    string description = null;
                    //Get atricle content, this is by ID. Only works for my site.
                    if (finalUrl.ToLower().Contains("tophattwaffle"))
                    {
                        description = htmlDocument.GetElementbyId("kb-article-content").InnerText;
                    }

                    description = description.Replace(@"&#8211;", "-").Replace("\n", "").Replace(@"&#8220;", "\"").Replace(@"&#8221;", "\"").Replace(@"&#8217;", "'");
                    title = title.Replace(@"&#8211;", "-").Replace("\n", "").Replace(" | TopHATTwaffle", "").Replace(@"&#8220;", "\"").Replace(@"&#8221;", "\"").Replace(@"&#8217;", "'");

                    //Limit length if needed
                    if (description.Length >= 180)
                    {
                        description = description.Substring(0, 180) + "...";
                    }

                    //Get images on the page
                    List<string> imgs = (from x in htmlDocument.DocumentNode.Descendants()
                                         where x.Name.ToLower() == "img"
                                         select x.Attributes["src"].Value).ToList<String>();

                    //Set image to the first non-header image if it exists.
                    string finalImg = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png";
                    if (imgs.Count > 1)
                        finalImg = imgs[_random.Next(0, imgs.Count)];

                    //Add results to list.
                    singleResult.Add(title);
                    singleResult.Add(finalUrl);
                    singleResult.Add(description);
                    singleResult.Add(finalImg);
                    listResults.Add(singleResult);
                }
            }
            catch(Exception)
            {
                //Do nothing. The command that called this will handle the no results found message.
            }
            return listResults;
        }

        async public Task GetPlayTestFiles(string[] testInfo, JsonServer server)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;

            if (server.FTPType.ToLower() == "ftps" || server.FTPType.ToLower() == "ftp")
                await DownloadFTPorFTPS(testInfo, server);
            if (server.FTPType.ToLower() == "sftp")
                await Task.Run(() => DownloadSFTP(testInfo, server));

            Console.ResetColor();
        }

        private async Task DownloadSFTP(string[] testInfo, JsonServer server)
        {
            DateTime time = Convert.ToDateTime(testInfo[1]);
            var workshopID = Regex.Match(testInfo[6], @"\d+$").Value;
            string demoName = $"{time.ToString("MM_dd_yyyy")}_{testInfo[2].Substring(0, testInfo[2].IndexOf(" "))}";
            string localPath = $"{demoPath}\\{time.ToString("yyyy")}\\{time.ToString("MM")} - {time.ToString("MMMM")}\\{demoName}";

            using (SftpClient sftp = new SftpClient(testInfo[10], server.FTPUser, server.FTPPass))
            {
                try
                {
                    sftp.Connect();
                    var files = sftp.ListDirectory(server.FTPPath);
                    SftpFile remoteDemoFile = null;
                    foreach (var file in files)
                    {
                        if (file.Name.ToLower().Contains(demoName.ToLower()))
                        {
                            remoteDemoFile = file;
                        }
                    }
                    files = sftp.ListDirectory($"{server.FTPPath}/maps/workshop/{workshopID}");
                    SftpFile remoteBSPFile = null;
                    foreach (var file in files)
                    {
                        if (file.Name.ToLower().Contains(".bsp"))
                        {
                            remoteBSPFile = file;
                        }
                    }
                    string localPathDemFile = $"{localPath}\\{remoteDemoFile.Name}";
                    string localPathBSPFile = $"{localPath}\\{remoteBSPFile.Name}";

                    Directory.CreateDirectory(localPath);
                    await ChannelLog($"Getting Demo File From Playtest",$"{remoteDemoFile.FullName}\n{localPathDemFile}");
                    using (Stream fileStreamDem = File.OpenWrite(localPathDemFile))
                    {
                        sftp.DownloadFile(remoteDemoFile.FullName, fileStreamDem);
                    }

                    await ChannelLog($"Getting BSP File From Playtest", $"{remoteBSPFile.FullName}\n{localPathBSPFile}");
                    using (Stream fileStreamBSP = File.OpenWrite(localPathBSPFile))
                    {
                        sftp.DownloadFile(remoteBSPFile.FullName, fileStreamBSP);
                    }

                    sftp.Disconnect();
                }
                catch (Exception e)
                {
                    Console.WriteLine("An exception has been caught " + e.ToString());
                }
            }

            var listFiles = Directory.GetFiles(localPath);
            await ChannelLog($"Download complete!", $"{string.Join("\n", listFiles)}");
        }

        //Download files over FTPS
        async private Task DownloadFTPorFTPS(string[] testInfo, JsonServer server)
        {
            using (FtpClient client = new FtpClient(server.Address, server.FTPUser, server.FTPPass))
            {
                if (server.FTPType == "ftps")
                {
                    client.EncryptionMode = FtpEncryptionMode.Explicit;
                    client.SslProtocols = SslProtocols.Tls;
                    client.ValidateCertificate += new FtpSslValidation(OnValidateCertificate);
                }
                await client.ConnectAsync();

                //Setup variables for getting the files.
                string workshopID = Regex.Match(testInfo[6], @"\d+$").Value;
                DateTime time = Convert.ToDateTime(testInfo[1]);
                string demoName = $"{time.ToString("MM_dd_yyyy")}_{testInfo[2].Substring(0, testInfo[2].IndexOf(" "))}";
                string localPath = $"{demoPath}\\{time.ToString("yyyy")}\\{time.ToString("MM")} - {time.ToString("MMMM")}\\{demoName}";
                string localPathDemoFile = $"{localPath}\\{demoName}_{testInfo[7]}.dem";

                //Make local directory for storing demo
                Directory.CreateDirectory(localPath);

                //Find the remote path to the demo file
                string[] fileList = client.GetNameListing(server.FTPPath);
                string demoFTPPath = null;
                foreach (var s in fileList)
                {
                    if (s.Contains($"{demoName.ToLower()}") || s.Contains($"{demoName}"))
                    {
                        demoFTPPath = s;
                    }
                }

                //Get the BSP paths
                string serverFTPBSPPath = $"{server.FTPPath}/maps/workshop/{workshopID}";
                fileList = client.GetNameListing(serverFTPBSPPath);
                string bspFTPPath = null;
                foreach (string s in fileList)
                {
                    if (s.Contains(".bsp"))
                        bspFTPPath = s;
                }
                string localPathBSPFile = $"{localPath}\\{Path.GetFileName(bspFTPPath)}";

                //Download Demo and BSP
                await ChannelLog($"Getting Demo File From Playtest", $"{demoFTPPath}\n{localPathDemoFile}");
                client.DownloadFile(localPathDemoFile, demoFTPPath);

                await ChannelLog($"Getting BSP File From Playtest", $"{bspFTPPath}\n{localPathBSPFile}");
                client.DownloadFile(localPathBSPFile, bspFTPPath);

                var listFiles = Directory.GetFiles(localPath);
                await ChannelLog($"Download complete!",$"{string.Join("\n", listFiles)}");
            }
        }

        private static void OnValidateCertificate(FtpClient control, FtpSslValidationEventArgs e)
        {
            e.Accept = true;
        }
    }
}
