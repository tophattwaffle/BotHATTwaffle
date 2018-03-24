using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;

using BotHATTwaffle.Extensions;
using BotHATTwaffle.Models;

using CoreRCON;

using Discord;
using Discord.WebSocket;

using HtmlAgilityPack;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BotHATTwaffle.Services
{
    public class DataService
    {
        public Dictionary<string, string> Config;

        private readonly DiscordSocketClient _client;
        private readonly Random _random;
        public string DemoPath;

        //Channels and Role vars
        private string _generalChannelStr;
        private string _logChannelStr;
        private string _playTesterRoleStr;
        private string _announcementChannelStr;
        private string _testingChannelStr;
        private string _modRoleStr;
        private string _mutedRoleStr;
        private string _rconRoleStr;
        private string _activeRoleStr;
        private string _patreonsRoleStr;
        private string _CommunityTesterRoleStr;
        public SocketTextChannel GeneralChannel { get; set; }
        public SocketTextChannel LogChannel { get; set; }
        public SocketTextChannel AnnouncementChannel  { get; set; }
        public SocketTextChannel TestingChannel  { get; set; }
        public SocketRole PlayTesterRole { get; set; }
        public SocketRole MuteRole { get; set; }
        public SocketRole RconRole { get; set; }
        public SocketRole ModRole { get; set; }
        public SocketRole ActiveRole { get; set; }
        public SocketRole PatreonsRole { get; set; }
        public SocketRole CommunityTesterRole { get; set; }

        //Misc setting vars
        public string[] PakRatEavesDrop;
        public string[] HowToPackEavesDrop;
        public string[] CarveEavesDrop;
        public string[] PropperEavesDrop;
        public string[] VbEavesDrop;
        public string[] AgreeEavesDrop;
        public string[] AgreeStrings;
        public string[] RoleMeWhiteList;
        public string CatFactPath;
        public string PenguinFactPath;
        public string TanookiFactPath;
        public string AlertUser;
        public int ShitPostDelay = 5;
        public static string dbPath;

        //TimerService Vars
        public int StartDelay = 10;
        public int UpdateInterval = 60;
        public string[] PlayingStrings;

        //Moderation Vars
        public string CasualConfig;
        public string CompConfig;
        public string PostConfig;

        //LevelTesting Vars
        public string[] PublicCommandWhiteList;
        public int CalUpdateTicks = 2;
        public string ImgurApi;

        public DataService(DiscordSocketClient client, Random random)
        {
            Config = ReadSettings(); //Needed when the data is first DI'd
            VariableAssignment();
            _client = client;
            _random = random;
        }

        /// <summary>
        /// Loads/Reloads all settings from settings.ini file.
        /// </summary>
        public void ReloadSettings()
        {
            //Read the text file
            Config = ReadSettings();

            //Reassign roles / Channels
            RoleChannelAssignments();

            //Assign the rest of the variables
            VariableAssignment();

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("SETTINGS HAVE BEEN LOADED\n");
            Console.ResetColor();
        }

        /// <summary>
        /// Deserialises, from a given JSON file, a token at a given path to an object of the given type.
        /// </summary>
        /// <typeparam name="TToken">The type of the object into which to deserialise the token.</typeparam>
        /// <param name="filePath">The path of the JSON file to deserialise.</param>
        /// <param name="tokenPath">The JPath expression to use to select the token.</param>
        /// <returns>
        /// The instance of the object into which the JSON was deserialised. If deserialisation fails, the default value of
        /// <typeparamref name="TToken"/> is returned.
        /// </returns>
        private static TToken DeserialiseToken<TToken>(string filePath, string tokenPath)
        {
            using (StreamReader file = File.OpenText(filePath))
            using (var reader = new JsonTextReader(file))
            {
                var obj = (JObject)JToken.ReadFrom(reader);

                try
                {
                    return obj.SelectToken(tokenPath).ToObject<TToken>();
                }
                catch (Exception e)
                {
                    // Could be JsonException, ArgumentException, or others. Documentation is poor in this respect; don't want to
                    // dig through source code to find others.
                    Console.WriteLine(
                        $"{e.GetType().Name}: Could not deserialise the token at the path {tokenPath} in the file {filePath} " +
                        $"for reason {e.Message}; a default value for the type {typeof(TToken).Name} will be returned.");

                    return default(TToken);
                }
            }
        }

        /// <summary>
        /// Handles the Settings.ini file.
        /// Creates keys with default settings if they do not exist.
        /// Reads in present keys and values.
        /// </summary>
        /// <returns>Dictionary with all the program's settings</returns>
        private Dictionary<string, string> ReadSettings()
        {
            string path = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            Dictionary<string, string> mainConfig;
            const string CONFIG_PATH = "settings.ini";
            if (File.Exists(CONFIG_PATH))
            {
                mainConfig = File.ReadAllLines(CONFIG_PATH).ToDictionary(line => line.Split('=')[0].Trim(), line => line.Split('=')[1].Trim());
            }
            else
            {
                // Config doesn't exist, so we'll make it
                mainConfig = new Dictionary<string, string>();
            }

            #region Add existing settings at their default
            #region General or global
            mainConfig.AddKeyIfMissing("botToken", "NEEDS_TO_BE_REPLACED");
            mainConfig.AddKeyIfMissing("imgurAPI", "NEEDS_TO_BE_REPLACED");
            mainConfig.AddKeyIfMissing("dbPath", "Master.sqlite");
            mainConfig.AddKeyIfMissing("startDelay", "10");
            mainConfig.AddKeyIfMissing("updateInterval", "60");
            mainConfig.AddKeyIfMissing("calUpdateTicks", "1");
            mainConfig.AddKeyIfMissing("logChannel", "bothattwaffle_logs");
            mainConfig.AddKeyIfMissing("announcementChannel", "announcements");
            mainConfig.AddKeyIfMissing("playingStringsCSV", "Eating Waffles,Not working on Titan,The year is 20XX,Hopefully not crashing,>help,>upcoming");
            mainConfig.AddKeyIfMissing("agreeUserCSV", "TopHATTwaffle,Phoby,thewhaleman,maxgiddens,CSGO John Madden,Wazanator,TanookiSuit3,JSadones,Lykrast,maxgiddens,Zelz Storm");
            mainConfig.AddKeyIfMissing("alertUser", "[DISCORD NAME OF USER WITH #]");
            mainConfig.AddKeyIfMissing("generalChannel", "general");
            #endregion

            #region Playtesting vars
            mainConfig.AddKeyIfMissing("testCalID", "Replace My Buddy");
            mainConfig.AddKeyIfMissing("playTesterRole", "Playtester");
            mainConfig.AddKeyIfMissing("activeMemberRole", "Active Member");
            mainConfig.AddKeyIfMissing("testingChannel", "csgo_level_testing");
            mainConfig.AddKeyIfMissing("DemoPath", $"X:\\Playtesting Demos");
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
            #endregion

            #region Command Dependent
            mainConfig.AddKeyIfMissing("roleMeWhiteListCSV", "Programmer,Level_Designer,3D_Modeler,Texture_Artist,Blender,Maya,3dsmax");
            mainConfig.AddKeyIfMissing("moderatorRoleName", "Moderators");
            mainConfig.AddKeyIfMissing("mutedRoleName", "Muted");
            mainConfig.AddKeyIfMissing("rconRoleName", "RconAccess");
            mainConfig.AddKeyIfMissing("publicCommandWhiteListCSV", "[CONFIGME]");
            mainConfig.AddKeyIfMissing("patreonsRole", "Patreons");
            mainConfig.AddKeyIfMissing("communityTestRole", "Community Tester");
            #endregion

            #region  Shitpost vars
            mainConfig.AddKeyIfMissing("catFactPath", $"X:\\Scripts\\catfacts.txt");
            mainConfig.AddKeyIfMissing("penguinFactPath", $"X:\\Scripts\\penguinfacts.txt");
            mainConfig.AddKeyIfMissing("tanookiFactPath", $"X:\\Scripts\\tanookifacts.txt");
            mainConfig.AddKeyIfMissing("ShitPostDelay", $"5");
            #endregion

            #endregion

            // Save new config file
            File.WriteAllLines(CONFIG_PATH, mainConfig.Select(kvp => $"{kvp.Key} = {kvp.Value}").ToArray());
            return mainConfig;
        }

        /// <summary>
        /// Assigns program variables to their values based on values in the config Dictionary
        /// </summary>
        private void VariableAssignment()
        {
            if (Config.ContainsKey("DemoPath"))
                DemoPath = Config["DemoPath"];
            if (Config.ContainsKey("dbPath"))
                dbPath = Config["dbPath"];
            if (Config.ContainsKey("pakRatEavesDropCSV"))
                PakRatEavesDrop = Config["pakRatEavesDropCSV"].Split(',');
            if (Config.ContainsKey("howToPackEavesDropCSV"))
                HowToPackEavesDrop = Config["howToPackEavesDropCSV"].Split(',');
            if (Config.ContainsKey("carveEavesDropCSV"))
                CarveEavesDrop = Config["carveEavesDropCSV"].Split(',');
            if (Config.ContainsKey("propperEavesDropCSV"))
                PropperEavesDrop = Config["propperEavesDropCSV"].Split(',');
            if (Config.ContainsKey("vbEavesDropCSV"))
                VbEavesDrop = Config["vbEavesDropCSV"].Split(',');
            if (Config.ContainsKey("agreeUserCSV"))
                AgreeEavesDrop = Config["agreeUserCSV"].Split(',');
            if (Config.ContainsKey("roleMeWhiteListCSV"))
                RoleMeWhiteList = Config["roleMeWhiteListCSV"].Split(',');
            if (Config.ContainsKey("startDelay") && !int.TryParse(Config["startDelay"], out StartDelay))
                Console.WriteLine($"Key \"startDelay\" not found or valid. Using default {StartDelay}.");

            if (Config.ContainsKey("updateInterval") && !int.TryParse(Config["updateInterval"], out UpdateInterval))
                Console.WriteLine($"Key \"updateInterval\" not found or valid. Using default {UpdateInterval}.");

            if (Config.ContainsKey("casualConfig"))
                CasualConfig = Config["casualConfig"];
            if (Config.ContainsKey("compConfig"))
                CompConfig = Config["compConfig"];
            if (Config.ContainsKey("postConfig"))
                PostConfig = Config["postConfig"];
            if (Config.ContainsKey("publicCommandWhiteListCSV"))
                PublicCommandWhiteList = Config["publicCommandWhiteListCSV"].Split(',');
            if (Config.ContainsKey("catFactPath"))
                CatFactPath = Config["catFactPath"];
            if (Config.ContainsKey("penguinFactPath"))
                PenguinFactPath = Config["penguinFactPath"];
            if (Config.ContainsKey("tanookiFactPath"))
                TanookiFactPath = Config["tanookiFactPath"];
            if (Config.ContainsKey("calUpdateTicks") && !int.TryParse(Config["calUpdateTicks"], out CalUpdateTicks))
                Console.WriteLine($"Key \"calUpdateTicks\" not found or valid. Using default {CalUpdateTicks}.");

            CalUpdateTicks = CalUpdateTicks - 1;

            if (Config.ContainsKey("ShitPostDelay") && !int.TryParse(Config["ShitPostDelay"], out ShitPostDelay))
                Console.WriteLine($"Key \"ShitPostDelay\" not found or valid. Using default {ShitPostDelay}.");

            if (Config.ContainsKey("playingStringsCSV"))
                PlayingStrings = Config["playingStringsCSV"].Split(',');
            if (Config.ContainsKey("alertUser"))
                AlertUser = Config["alertUser"];
            if (Config.ContainsKey("imgurAPI"))
                ImgurApi = Config["imgurAPI"];

        }

        /// <summary>
        /// Assigns roles and channels to variables
        /// </summary>
        private void RoleChannelAssignments()
        {
            if (Config.ContainsKey("generalChannel"))
                _generalChannelStr = Config["generalChannel"];

            if (Config.ContainsKey("announcementChannel"))
                _announcementChannelStr = Config["announcementChannel"];

            if (Config.ContainsKey("logChannel"))
                _logChannelStr = Config["logChannel"];

            if (Config.ContainsKey("testingChannel"))
                _testingChannelStr = Config["testingChannel"];

            if (Config.ContainsKey("playTesterRole"))
                _playTesterRoleStr = Config["playTesterRole"];

            if (Config.ContainsKey("moderatorRoleName"))
                _modRoleStr = Config["moderatorRoleName"];

            if (Config.ContainsKey("mutedRoleName"))
                _mutedRoleStr = Config["mutedRoleName"];

            if (Config.ContainsKey("rconRoleName"))
                _rconRoleStr = Config["rconRoleName"];

            if (Config.ContainsKey("activeMemberRole"))
                _activeRoleStr = Config["activeMemberRole"];

            if (Config.ContainsKey("patreonsRole"))
                _patreonsRoleStr = Config["patreonsRole"];

            if (Config.ContainsKey("communityTestRole"))
                _CommunityTesterRoleStr = Config["communityTestRole"];

            var arg = _client.Guilds.FirstOrDefault();

            Console.ForegroundColor = ConsoleColor.Green;
            //Iterate all channels
            foreach (SocketTextChannel s in arg.TextChannels)
            {
                if (s.Name == _logChannelStr)
                {
                    LogChannel = s;
                    Console.WriteLine($"\nLog Channel Found! Logging to: {s.Name}\nID: {s.Id}");
                }
                if (s.Name == _announcementChannelStr)
                {
                    AnnouncementChannel = s;
                    Console.WriteLine($"\nAnnouncement Channel Found! Announcing to: {s.Name}\nID: {s.Id}");
                }
                if (s.Name == _testingChannelStr)
                {
                    TestingChannel = s;
                    Console.WriteLine($"\nTesting Channel Found! Sending playtest alerts to: {s.Name}\nID: {s.Id}");
                }
                if (s.Name == _generalChannelStr)
                {
                    GeneralChannel = s;
                    Console.WriteLine($"\nGeneral Channel Found! Using this channel for general use: {s.Name}\nID: {s.Id}");
                }
            }

            foreach (SocketRole r in arg.Roles)
            {
                if (r.Name == _playTesterRoleStr)
                {
                    PlayTesterRole = r;
                    Console.WriteLine($"\nPlaytester role found!: {r.Name}\nID: {r.Id}");
                }
                if (r.Name == _modRoleStr)
                {
                    ModRole = r;
                    Console.WriteLine($"\nModerator role found!: {r.Name}\nID: {r.Id}");
                }
                if (r.Name == _rconRoleStr)
                {
                    RconRole = r;
                    Console.WriteLine($"\nRCON role found!: {r.Name}\nID: {r.Id}");
                }
                if (r.Name == _mutedRoleStr)
                {
                    MuteRole = r;
                    Console.WriteLine($"\nMuted role found!: {r.Name}\nID: {r.Id}");
                }
                if (r.Name == _activeRoleStr)
                {
                    ActiveRole = r;
                    Console.WriteLine($"\nActive Memeber role found!: {r.Name}\nID: {r.Id}"); //That isn't a spelling mistake :kappa:
                }
                if (r.Name == this._patreonsRoleStr)
                {
                    PatreonsRole = r;
                    Console.WriteLine($"\nPatreons role found!: {r.Name}\nID: {r.Id}");
                }
                if (r.Name == this._CommunityTesterRoleStr)
                {
                    CommunityTesterRole = r;
                    Console.WriteLine($"\nCommunity Tester Role role found!: {r.Name}\nID: {r.Id}");
                }

            }
            Console.WriteLine();
            Console.ResetColor();
        }

        /// <summary>
        /// Logs a message to the log channel
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="mention">Alert TopHATTwaffle?</param>
        /// <returns>Task</returns>
        public Task ChannelLog(string message, bool mention = false)
        {
            string alert = null;
            if (mention)
            {
                var splitUser = AlertUser.Split('#');
                alert = _client.GetUser(splitUser[0], splitUser[1]).Mention;
            }

            LogChannel.SendMessageAsync($"{alert}```{DateTime.Now}\n{message}```");
            Console.WriteLine($"{DateTime.Now}: {message}\n");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Logs a message to the log channel
        /// </summary>
        /// <param name="title">Title of log</param>
        /// <param name="message">Message to log</param>
        /// <param name="mention">Alert TopHATTwaffle?</param>
        /// <returns>Task</returns>
        public Task ChannelLog(string title, string message, Boolean mention = false)
        {
            string alert = null;
            if (mention)
            {
                var splitUser = AlertUser.Split('#');
                alert = _client.GetUser(splitUser[0], splitUser[1]).Mention;
            }

            LogChannel.SendMessageAsync($"{alert}```{DateTime.Now}\n{title}\n{message}```");
            Console.WriteLine($"{DateTime.Now}: {title}\n{message}\n");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Takes a 3 letter server prefix to find the correct server
        /// </summary>
        /// <param name="serverStr">3 letter server code</param>
        /// <returns>Server object that was located in Database</returns>
        public Server GetServer(string serverStr)
        {
            return DataBaseUtil.GetServer(serverStr);
        }

        /// <summary>
        /// Gets all the servers that exist in the servers JSON file
        /// and gives you an embed that lets you visualize their information.
        /// </summary>
        /// <returns>Embed object with server information</returns>
        public Discord.Embed GetAllServers()
        {
            var authBuilder = new EmbedAuthorBuilder()
            {
                Name = $"Server List",
                IconUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
            };

            List<Server> servers = DataBaseUtil.GetAllServer();

            List<EmbedFieldBuilder> fieldBuilder = new List<EmbedFieldBuilder>();
            foreach (var s in servers)
            {
                fieldBuilder.Add(new EmbedFieldBuilder { Name = $"{s.address}", Value = $"Prefix: `{s.name}`\n{s.description}", IsInline = false });
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

        /// <summary>
        /// Sends a RCON command to a server
        /// </summary>
        /// <param name="command">Command to send</param>
        /// <param name="server">3 letter server code</param>
        /// <returns>Output from RCON command</returns>
        public async Task<string> RconCommand(string command, Server server)
        {
            string reply = null;

            //Get the bots IP, typically my IP
            string botIp = new WebClient().DownloadString("http://icanhazip.com").Trim();
            IPHostEntry iPHostEntry = null;
            try
            {
                iPHostEntry = Dns.GetHostEntry(server.address);
            }
            catch
            {
                return "HOST_NOT_FOUND";
            }

            //Setup new RCON object
            using (var rcon = new RCON(IPAddress.Parse($"{iPHostEntry.AddressList[0]}"), 27015, server.rcon_password))
            {
                //Send the RCON command to the server
                reply = await rcon.SendCommandAsync(command);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{DateTime.Now}\nRCON COMMAND: {server.address}\nCommand: {command}\n");
                Console.ResetColor();
            }

            //Remove the Bot's public IP from the string.
            reply = reply.Replace($"{botIp}", "69.420.MLG.1337");

            return reply;
        }

        /// <summary>
        /// Searches for a tutorial or the FAQ
        /// </summary>
        /// <param name="searchSeries">What series to search</param>
        /// <param name="searchTerm">What term to search</param>
        /// <param name="isPrivate">Was this invoked from a DM?</param>
        /// <returns>Returns a 2D list of strings</returns>
        public List<List<string>> Search(string searchSeries, string searchTerm, bool isPrivate)
        {
            List<SearchDataResult> foundData = new List<SearchDataResult>();
            List<List<string>> listResults = new List<List<string>>();

            //Let's us search on multiple terms at a time
            string[] searchTermArray = searchTerm.Split(' ');

            //Did we search FAQ?
            if (searchSeries.ToLower() == "faq" || searchSeries.ToLower() == "f" || searchSeries.ToLower() == "7")
                return SearchFaq(searchTerm, isPrivate);

            switch (searchSeries)
            {
                case "v2series":
                case "v2":
                case "1":
                    foundData = DataBaseUtil.GetSearchInformation(searchTermArray, "v2");
                    break;
                case "csgobootcamp":
                case "bc":
                case "2":
                    foundData = DataBaseUtil.GetSearchInformation(searchTermArray, "bc");
                    break;
                case "3dsmax":
                case "3ds":
                case "3":
                    foundData = DataBaseUtil.GetSearchInformation(searchTermArray, "3ds");
                    break;
                case "writtentutorials":
                case "written":
                case "4":
                    foundData = DataBaseUtil.GetSearchInformation(searchTermArray, "written");
                    break;
                case "legacyseries":
                case "v1":
                case "lg":
                case "5":
                    foundData = DataBaseUtil.GetSearchInformation(searchTermArray, "lg");
                    break;
                case "hammertroubleshooting":
                case "ht":
                case "6":
                case "misc":
                    foundData = DataBaseUtil.GetSearchInformation(searchTermArray, "ht");
                    break;
                case "all":
                    foundData = DataBaseUtil.GetSearchInformation(searchTermArray, "all");
                    break;
                default:
                    //do nothing
                    break;
            }

            //Process each result that was located
            foreach (var result in foundData)
            {
                List<string> singleResult = new List<string>();

                //Limit to 3 FAQ results.
                //Then let's add another one with a direct link to the page. Only limit for non-DM
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

                //Create a HTML client so we can get info about the link
                HtmlWeb htmlWeb = new HtmlWeb();
                HtmlDocument htmlDocument = htmlWeb.Load(result.url);
                string title = null;

                //Processing for non-YouTube URLs
                if (!result.url.Contains("youtube"))
                {
                    //Get the page title
                    title = (from x in htmlDocument.DocumentNode.Descendants()
                             where x.Name.ToLower() == "title"
                             select x.InnerText).FirstOrDefault();
                }
                //Processing for YouTube URLs
                else if (result.url.ToLower().Contains("youtube"))
                    title = GetYouTubeTitle(result.url);

                string description = null;
                //Get article content, this is by ID. Only works for my site.
                if (result.url.ToLower().Contains("tophattwaffle"))
                    description = htmlDocument.GetElementbyId("content-area").InnerText;
                else if(result.url.ToLower().Contains("youtube"))
                    description = result.url;

                //Only if not Null - Fix the bad characters that get pulled from the web page.
                description = description?.Replace(@"&#8211;", "-").Replace("\n", "").Replace(@"&#8220;", "\"").Replace(@"&#8221;", "\"").Replace(@"&#8217;", "'");
                title = title?.Replace(@"&#8211;", "-").Replace("\n", "").Replace(" | TopHATTwaffle", "").Replace(@"&#8220;", "\"").Replace(@"&#8221;", "\"").Replace(@"&#8217;", "'");

                //Limit length if needed
                if (description != null && description.Length >= 250)
                    description = description.Substring(0, 250) + "...";

                //Get images on the page
                List<string> imgs = null;

                if (!result.url.ToLower().Contains("youtube"))
                {
                imgs = (from x in htmlDocument.DocumentNode.Descendants()
                                     where x.Name.ToLower() == "img"
                                     select x.Attributes["src"].Value).ToList<string>();
                }

                //Set image to the first non-header image if it exists.
                string finalImg = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png";
                if (imgs != null && imgs.Count > 1)
                    finalImg = imgs[_random.Next(0, imgs.Count)];

                if (result.url.Contains("youtube"))
                {
                    finalImg = GetYouTubeImage(result.url);
                }

                //Add results to list
                singleResult.Add(title);
                singleResult.Add(result.url);
                singleResult.Add(description);
                singleResult.Add(finalImg);
                listResults.Add(singleResult);
            }
            return listResults;
        }

        /// <summary>
        /// Gets a title from a YouTube URL
        /// </summary>
        /// <param name="url">Video URL</param>
        /// <returns>YouTube Video Title</returns>
        public static string GetYouTubeTitle(string url)
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

        /// <summary>
        /// Gets the thumbnail for a youtube URL
        /// </summary>
        /// <param name="videoUrl">Video URL</param>
        /// <returns>Path to Thumbnail Image</returns>
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

        /// <summary>
        /// Searches FAQ
        /// </summary>
        /// <param name="searchTerm">Term to search</param>
        /// <param name="isPrivate">Is in DM?</param>
        /// <returns>Returns a 2D list of strings</returns>
        public List<List<string>> SearchFaq(string searchTerm, bool isPrivate)
        {
            List<List<string>> listResults = new List<List<string>>();

            const string FAQURL = "https://www.tophattwaffle.com/wp-admin/admin-ajax.php?action=epkb-search-kb&epkb_kb_id=1&search_words=";
            try
            {
                //New web client
                HtmlWeb faqWeb = new HtmlWeb();

                //Let's load the search
                HtmlDocument faqDocument = faqWeb.Load($"{FAQURL}{searchTerm}");

                //Look at all links that the page has on it
                foreach (HtmlNode link in faqDocument.DocumentNode.SelectNodes("//a[@href]"))
                {
                    List<string> singleResult = new List<string>();

                    //Limit to 3 FAQ results. Let's add another one with a direct link to the page.  Only limit for non-DM.
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

                    //Get article content, this is by ID. Only works for my site.
                    string description = null;
                    if (finalUrl.ToLower().Contains("tophattwaffle"))
                    {
                        description = htmlDocument.GetElementbyId("kb-article-content").InnerText;
                    }

                    //Only if not Null - Fix the bad characters that get pulled from the web page.
                    description = description?.Replace(@"&#8211;", "-").Replace("\n", "").Replace(@"&#8220;", "\"").Replace(@"&#8221;", "\"").Replace(@"&#8217;", "'");
                    title = title?.Replace(@"&#8211;", "-").Replace("\n", "").Replace(" | TopHATTwaffle", "").Replace(@"&#8220;", "\"").Replace(@"&#8221;", "\"").Replace(@"&#8217;", "'");

                    //Limit length if needed
                    if (description != null && description.Length >= 180)
                        description = description.Substring(0, 180) + "...";

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

        /// <summary>
        /// Provided a URL, will scan the page for all files that end in a file
        /// It then picks one at random and returns that
        /// Example Page: https://content.tophattwaffle.com/BotHATTwaffle/catfacts/
        /// </summary>
        /// <param name="inUrl">URL to look at</param>
        /// <returns>inUrl + ImageName.ext</returns>
        public string GetRandomImgFromUrl(string inUrl)
        {
            //New web client
            HtmlWeb htmlWeb = new HtmlWeb();

            //Load page
            HtmlDocument htmlDocument = htmlWeb.Load(inUrl);

            //Add each image to a list
            List<string> validImg = htmlDocument.DocumentNode.SelectNodes("//a[@href]").Select(link =>
                link.GetAttributeValue("href", string.Empty).Replace(@"\", "").Replace("\"", "")).Where(Path.HasExtension).ToList();

            return inUrl + validImg[(_random.Next(0, validImg.Count))];
        }
    }
}
