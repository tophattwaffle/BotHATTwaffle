using System;
using System.Collections.Generic;
using System.ComponentModel;
using BotHATTwaffle.Modules.Json;

namespace BotHATTwaffle.Objects.Downloader {
    public class DownloaderService
    {
        private readonly DataServices dataSvc;
        private readonly BackgroundWorker worker;

        public DownloaderService(DataServices dataSvc)
        {
            this.dataSvc = dataSvc;
            worker = new BackgroundWorker();

            // DownloadFiles is used as the DoWork event.
            worker.DoWork += DownloadFiles;
        }

        public void Start(IReadOnlyList<string> testInfo, JsonServer server)
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
            var (testInfo, server) =
                (ValueTuple<IReadOnlyList<string>, JsonServer>) e.Argument;

            switch (server.FTPType.ToLower())
            {
                case "ftps":
                case "ftp":
                    using (var dl = new FtpDownloader(testInfo, server,
                                                      dataSvc))
                    {
                        dl.Download();
                    }

                    break;
                case "sftp":
                    using (var dl = new SftpDownloader(testInfo, server,
                                                       dataSvc))
                    {
                        dl.Download();
                    }

                    break;
            }

            Console.ResetColor();
        }
    }
}
