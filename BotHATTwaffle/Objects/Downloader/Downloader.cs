using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BotHATTwaffle.Modules.Json;

namespace BotHATTwaffle.Objects.Downloader
{
    public abstract class Downloader<TClient> : IDisposable
        where TClient : IDisposable
    {
        protected TClient Client { get; set; }

        protected string DemoName { get; }

        protected string FtpPath { get; }

        protected string LocalPath { get; }

        protected string WorkshopId { get; }

        protected Downloader(IReadOnlyList<string> testInfo,
                             JsonServer server,
                             string demoPath)
        {
            DateTime time = Convert.ToDateTime(testInfo[1]);
            string title = testInfo[2].Substring(0, testInfo[2].IndexOf(" "));

            DemoName = $"{time:MM_dd_yyyy}_{title}";
            FtpPath = server.FTPPath;
            LocalPath = $"{demoPath}\\{time:yyyy}\\{time:MM} - " +
                        $"{time:MMMM}\\{DemoName}";
            WorkshopId = Regex.Match(testInfo[6], @"\d+$").Value;
        }

        public abstract void Download();

        public void Dispose()
        {
            Client.Dispose();
        }
    }
}
