using System;
using System.Collections.Generic;
using System.ComponentModel;
using BotHATTwaffle.Modules.Json;

namespace BotHATTwaffle.Objects.Downloader {
    public class DownloaderService {
        private DataServices DataSvc { get; }

        private BackgroundWorker Worker { get; }

        public DownloaderService(DataServices ds)
        {
            DataSvc = ds;
            Worker = new BackgroundWorker();

            // DownloadFiles is used as the DoWork event.
            Worker.DoWork += DownloadFiles;
        }

        public void Start(IReadOnlyList<string> testInfo, JsonServer server)
        {
            if (!Worker.IsBusy)
            {
                // RunWorkerAsync raises the DoWork event.
                Worker.RunWorkerAsync((DataSvc, testInfo, server));
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
                                                      DataSvc.DemoPath))
                    {
                        dl.Download();
                    }

                    break;
                case "sftp":
                    using (var dl = new SftpDownloader(testInfo, server,
                                                       DataSvc.DemoPath))
                    {
                        dl.Download();
                    }

                    break;
            }

            Console.ResetColor();
        }
    }
}
