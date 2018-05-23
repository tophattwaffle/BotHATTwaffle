using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;

using BotHATTwaffle.Commands.Readers;
using BotHATTwaffle.Models;

using CoreRCON;

using Discord;
using Discord.WebSocket;

using HtmlAgilityPack;

namespace BotHATTwaffle.Services
{
    public class DataService
    {
        private static readonly ImmutableDictionary<string, string> _DefaultConfig = new Dictionary<string, string>
        {
            #region Default Config

            {"botToken", null},
            {"imgurAPI", null},
            {"testCalID", null},
            {"startDelay", "10"},
            {"updateInterval", "60"},
            {"ShitPostDelay", "5"},
            {"calUpdateTicks", "1"},
            {"alertUser", null},

            // Channels
            {"generalChannel", "general"},
            {"announcementChannel", "announcements"},
            {"testingChannel", "csgo_level_testing"},
            {"logChannel", "bothattwaffle_logs"},

            // Paths
            {"DemoPath", @".\Demos"},
            {"catFactPath", string.Empty},
            {"penguinFactPath", string.Empty},
            {"tanookiFactPath", string.Empty},

            // Playtesting
            {"casualConfig", "thw"},
            {"compConfig", "thw"},
            {"postConfig", "postame"},

            // CSVs
            {"publicCommandWhiteListCSV", string.Empty},
            {"playingStringsCSV", string.Empty},
            {"roleMeWhiteListCSV", string.Empty},
            {"pakRatEavesDropCSV", string.Empty},
            {"howToPackEavesDropCSV", string.Empty},
            {"carveEavesDropCSV", string.Empty},
            {"propperEavesDropCSV", string.Empty},
            {"vbEavesDropCSV", string.Empty},
            {"agreeStringsCSV", string.Empty},
            {"agreeUserCSV", string.Empty}

            #endregion
        }.ToImmutableDictionary();

        private readonly DiscordSocketClient _client;
        private readonly Random _random;

        public ImmutableDictionary<string, string> Config { get; private set; }

        // Miscellaneous
        public string AlertUser { get; private set; }
        public IImmutableSet<SocketRole> RoleMeWhiteList { get; private set; } = ImmutableHashSet<SocketRole>.Empty;

        // Channels
        public SocketTextChannel GeneralChannel { get; private set; }
        public SocketTextChannel LogChannel { get; private set; }
        public SocketTextChannel AnnouncementChannel { get; private set; }
        public SocketTextChannel TestingChannel { get; private set; }

        // Roles
        public SocketRole PlayTesterRole { get; private set; }
        public SocketRole MuteRole { get; private set; }
        public SocketRole RconRole { get; private set; }
        public SocketRole ModRole { get; private set; }
        public SocketRole ActiveRole { get; private set; }
        public SocketRole PatreonsRole { get; private set; }
        public SocketRole CommunityTesterRole { get; private set; }

        // MessageListener
        public IImmutableSet<string> PakRatEavesDrop { get; private set; } = ImmutableHashSet<string>.Empty;
        public IImmutableSet<string> HowToPackEavesDrop { get; private set; } = ImmutableHashSet<string>.Empty;
        public IImmutableSet<string> CarveEavesDrop { get; private set; } = ImmutableHashSet<string>.Empty;
        public IImmutableSet<string> PropperEavesDrop { get; private set; } = ImmutableHashSet<string>.Empty;
        public IImmutableSet<string> VbEavesDrop { get; private set; } = ImmutableHashSet<string>.Empty;

        // Shitposts
        public string CatFactPath { get; private set; }
        public string PenguinFactPath { get; private set; }
        public string TanookiFactPath { get; private set; }
        public IImmutableSet<ulong> ShitpostAgreeUserIds { get; private set; } = ImmutableHashSet<ulong>.Empty;
        public IImmutableSet<string> ShitpostAgreeReplies { get; private set; } = ImmutableHashSet<string>.Empty;
        public int ShitPostDelay { get; private set; } = 5;

        // TimerService
        public int StartDelay { get; private set; } = 10;
        public int UpdateInterval { get; private set; } = 60;
        public IImmutableSet<string> PlayingStrings { get; private set; } = ImmutableHashSet<string>.Empty;

        // Playtesting
        public string CasualConfig { get; private set; }
        public string CompConfig { get; private set; }
        public string PostConfig { get; private set; }
        public IImmutableSet<string> PublicCommandWhiteList { get; private set; } = ImmutableHashSet<string>.Empty;
        public int CalUpdateTicks { get; private set; } = 2;
        public string ImgurApi { get; private set; }
        public string DemoPath { get; private set; }
        public string CalendarId { get; private set; }

        public DataService(DiscordSocketClient client, Random random)
        {
            _client = client;
            _random = random;

            // Some settings are needed before the client connects (e.g. token).
            ReadConfig();
            DeserialiseSettings();
        }

        /// <summary>
        /// Reads the configuration file into a dictionary.
        /// <para>
        /// Looks for a file named <c>settings.ini</c> in the executable's directory. If it doesn't exist, it is created with
        /// default values.
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the config is incorrectly formatted or has missing values.
        /// </exception>
        /// <returns>Dictionary with all the program's settings</returns>
        private void ReadConfig()
        {
            var dict = new Dictionary<string, string>();
            const string CONFIG_PATH = "settings.ini";

            using (var fs = new FileStream(CONFIG_PATH, FileMode.OpenOrCreate, FileAccess.Read))
            using (var stream = new StreamReader(fs))
            {
                string line = stream.ReadLine();

                for (var i = 1U; line != null; ++i, line = stream.ReadLine())
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    string[] kv = line.Split('=');
                    string key = kv[0].Trim(); // 0th index always exists.
                    string value = kv.ElementAtOrDefault(1)?.Trim();

                    if (kv.Length > 2 || string.IsNullOrWhiteSpace(key))
                        throw new InvalidOperationException($"Error reading config at line {i}: invalid format.");

                    bool keyExists = _DefaultConfig.TryGetValue(key, out string defaultVal);

                    if (string.IsNullOrWhiteSpace(value) && keyExists)
                    {
                        if (defaultVal == null)
                        {
                            throw new InvalidOperationException(
                                $"Error reading config at line {i}: mandatory key '{key}' has no value.");
                        }

                        if (defaultVal.Length > 0)
                        {
                            value = defaultVal;
                            Console.WriteLine(
                                $"Warning reading config at line {i}: mandatory key '{key}' has no value. " +
                                $"Using default of '{defaultVal}'.");
                        }
                    }

                    if (keyExists)
                        dict.Add(key, value);
                }
            }

            var missing = _DefaultConfig.Where(kv => !dict.ContainsKey(kv.Key)).ToImmutableDictionary();
            var mandatory = missing.Where(kv => _DefaultConfig[kv.Key] == null).ToImmutableDictionary();
            dict = dict.Concat(missing).ToDictionary(kv => kv.Key, kv => kv.Value);

            // Saves the new config file.
            File.WriteAllLines(CONFIG_PATH, dict.Select(kv => $"{kv.Key} = {kv.Value}"));

            if (missing.Count > mandatory.Count)
                Console.WriteLine("Some config keys are missing and have been written with defaults values.");

            if (mandatory.Any())
            {
                throw new InvalidOperationException(
                    "The following mandatory config keys are missing and have no default values. " +
                    "Configure the values and restart/reload the bot.\n" +
                    string.Join("\n", mandatory.Keys));
            }

            Config = dict.ToImmutableDictionary();
        }

        /// <summary>
        /// Deserialises the configuration file into fields.
        /// </summary>
        /// <param name="reread"><c>true</c> if the config file should be re-read; <c>false</c> otherwise.</param>
        /// <exception cref="InvalidOperationException">Thrown when the config fails to be read or deserialised.</exception>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        public async Task DeserialiseConfig(bool reread = false)
        {
            if (reread)
            {
                ReadConfig();
                DeserialiseSettings();
            }

            await DeserialiseChannels();
            await GetRoles();

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("SETTINGS HAVE BEEN LOADED\n");
            Console.ResetColor();
        }

        /// <summary>
        /// Deserialises settings from configuraton file.
        /// </summary>
        private void DeserialiseSettings()
        {
            CalendarId = Config["testCalID"];
            DemoPath = Config["DemoPath"];
            CasualConfig = Config["casualConfig"];
            CompConfig = Config["compConfig"];
            PostConfig = Config["postConfig"];
            CatFactPath = Config["catFactPath"];
            PenguinFactPath = Config["penguinFactPath"];
            TanookiFactPath = Config["tanookiFactPath"];
            AlertUser = Config["alertUser"];
            ImgurApi = Config["imgurAPI"];
            PakRatEavesDrop = Config["pakRatEavesDropCSV"].Split(',').Select(v => v.Trim()).ToImmutableHashSet();
            HowToPackEavesDrop = Config["howToPackEavesDropCSV"].Split(',').Select(v => v.Trim()).ToImmutableHashSet();
            CarveEavesDrop = Config["carveEavesDropCSV"].Split(',').Select(v => v.Trim()).ToImmutableHashSet();
            PropperEavesDrop = Config["propperEavesDropCSV"].Split(',').Select(v => v.Trim()).ToImmutableHashSet();
            VbEavesDrop = Config["vbEavesDropCSV"].Split(',').Select(v => v.Trim()).ToImmutableHashSet();
            ShitpostAgreeReplies = Config["agreeStringsCSV"].Split(',').Select(v => v.Trim()).ToImmutableHashSet();
            PublicCommandWhiteList = Config["publicCommandWhiteListCSV"].Split(',').Select(v => v.Trim()).ToImmutableHashSet();
            PlayingStrings = Config["playingStringsCSV"].Split(',').Select(v => v.Trim()).ToImmutableHashSet();

            if (int.TryParse(Config["startDelay"], out int temp))
                StartDelay = temp;
            else
                Console.WriteLine($"Key 'startDelay' not found or valid. Using default {StartDelay}.");

            if (int.TryParse(Config["updateInterval"], out temp))
                UpdateInterval = temp;
            else
                Console.WriteLine($"Key 'updateInterval' not found or valid. Using default {UpdateInterval}.");

            if (int.TryParse(Config["ShitPostDelay"], out temp))
                ShitPostDelay = temp;
            else
                Console.WriteLine($"Key 'ShitPostDelay' not found or valid. Using default {ShitPostDelay}.");

            if (int.TryParse(Config["calUpdateTicks"], out temp))
                CalUpdateTicks = temp;
            else
                Console.WriteLine($"Key 'calUpdateTicks' not found or valid. Using default {CalUpdateTicks}.");

            CalUpdateTicks -= 1;

            var agreeUserIds = new HashSet<ulong>();

            foreach (string idStr in Config["agreeUserCSV"].Split(','))
            {
                if (ulong.TryParse(idStr.Trim(), out ulong id))
                    agreeUserIds.Add(id);
            }

            ShitpostAgreeUserIds = agreeUserIds.ToImmutableHashSet();
        }

        /// <summary>
        /// Deserialises channels from the configuration file.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when a channel can't be found.</exception>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        private async Task DeserialiseChannels()
        {
            SocketGuild guild = _client.Guilds.FirstOrDefault();

            LogChannel = await ParseChannel("logChannel");
            AnnouncementChannel = await ParseChannel("announcementChannel");
            TestingChannel = await ParseChannel("testingChannel");
            GeneralChannel = await ParseChannel("generalChannel");

            async Task<SocketTextChannel> ParseChannel(string key)
            {
                SocketTextChannel channel = await ChannelTypeReader<SocketTextChannel>.GetBestResultAsync(guild, Config[key]);

                if (channel == null)
                    throw new InvalidOperationException($"The value of key '{key}' could not be parsed as a channel.");

                return channel;
            }
        }

        /// <summary>
        /// Retrieves role socket entities from the IDs in the <see cref="Role"/> enum.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when a role can't be found.</exception>
        private async Task GetRoles()
        {
            SocketGuild guild = _client.Guilds.FirstOrDefault();

            PlayTesterRole = GetRole(Role.Playtester);
            ModRole = GetRole(Role.Moderators);
            RconRole = GetRole(Role.RconAccess);
            MuteRole = GetRole(Role.Muted);
            ActiveRole = GetRole(Role.ActiveMember);
            PatreonsRole = GetRole(Role.Patreons);
            CommunityTesterRole = GetRole(Role.CommunityTester);

            var roleMeRoles = new HashSet<SocketRole>();

            foreach (string role in Config["roleMeWhiteListCSV"].Split(','))
                roleMeRoles.Add(await ParseRole(role));

            RoleMeWhiteList = roleMeRoles.ToImmutableHashSet();

            SocketRole GetRole(Role role)
            {
                SocketRole r = guild?.GetRole((ulong)role);

                if (r == null)
                    throw new InvalidOperationException($"The role '{role}' could not be found.");

                return r;
            }

            async Task<SocketRole> ParseRole(string name)
            {
                SocketRole role = await RoleTypeReader<SocketRole>.GetBestResultAsync(guild, name);

                if (role == null)
                    throw new InvalidOperationException($"'{name}' could not be parsed as a role.");

                return role;
            }
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
            if (mention && !string.IsNullOrWhiteSpace(AlertUser))
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
        public Task ChannelLog(string title, string message, bool mention = false)
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
        public async Task<Server> GetServer(string serverStr)
        {
            return await DataBaseUtil.GetServerAsync(serverStr);
        }

        /// <summary>
        /// Gets all the servers that exist in the servers JSON file
        /// and gives you an embed that lets you visualize their information.
        /// </summary>
        /// <returns>Embed object with server information</returns>
        public async Task<Discord.Embed> GetAllServers()
        {
            var authBuilder = new EmbedAuthorBuilder()
            {
                Name = $"Server List",
                IconUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
            };

            Server[] servers = await DataBaseUtil.GetServersAsync();

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
        public async Task<List<List<string>>> Search(string searchSeries, string searchTerm, bool isPrivate)
        {
            var foundData = new ImmutableArray<SearchDataResult>();
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
                    foundData = await DataBaseUtil.GetTutorialsAsync(searchTermArray, "v2");
                    break;
                case "csgobootcamp":
                case "bc":
                case "2":
                    foundData = await DataBaseUtil.GetTutorialsAsync(searchTermArray, "bc");
                    break;
                case "3dsmax":
                case "3ds":
                case "3":
                    foundData = await DataBaseUtil.GetTutorialsAsync(searchTermArray, "3ds");
                    break;
                case "writtentutorials":
                case "written":
                case "4":
                    foundData = await DataBaseUtil.GetTutorialsAsync(searchTermArray, "written");
                    break;
                case "legacyseries":
                case "v1":
                case "lg":
                case "5":
                    foundData = await DataBaseUtil.GetTutorialsAsync(searchTermArray, "lg");
                    break;
                case "hammertroubleshooting":
                case "ht":
                case "6":
                case "misc":
                    foundData = await DataBaseUtil.GetTutorialsAsync(searchTermArray, "ht");
                    break;
                case "all":
                    foundData = await DataBaseUtil.GetTutorialsAsync(searchTermArray, "all");
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
