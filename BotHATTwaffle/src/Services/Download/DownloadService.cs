using System;
using System.Collections.Generic;
using System.ComponentModel;

using BotHATTwaffle.Models;

namespace BotHATTwaffle.Services.Download
{
    public class DownloadService
    {
        private readonly DataService dataSvc;
        private readonly BackgroundWorker worker;

        public DownloadService(DataService dataSvc)
        {
            this.dataSvc = dataSvc;
            worker = new BackgroundWorker();

            // DownloadFiles is used as the DoWork event.
            worker.DoWork += DownloadFiles;
        }

        public void Start(IReadOnlyList<string> testInfo, Server server)
        {
            if (!worker.IsBusy)
            {
                // RunWorkerAsync raises the DoWork event.
                worker.RunWorkerAsync((testInfo, server));
            }
        }

        private void DownloadFiles(object sender, DoWorkEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            var (testInfo, server) = (ValueTuple<IReadOnlyList<string>, Server>) e.Argument;

            switch (server.ftp_type.ToLower())
            {
                case "ftps":
                case "ftp":
                    using (var dl = new FtpDownloader(testInfo, server, dataSvc))
                    {
                        dl.Download();
                    }

                    break;
                case "sftp":
                    using (var dl = new SftpDownloader(testInfo, server, dataSvc))
                    {
                        dl.Download();
                    }

                    break;
            }

            Console.ResetColor();
        }
    }
}
