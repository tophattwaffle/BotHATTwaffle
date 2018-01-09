using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BotHATTwaffle.Modules.Json;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace BotHATTwaffle.Objects.Downloader {
    public sealed class SftpDownloader : Downloader<SftpClient> {
        public SftpDownloader(IReadOnlyList<string> testInfo,
                              JsonServer server,
                              string demoPath) :
            base(testInfo, server, demoPath)
        {
            Client =
                new SftpClient(testInfo[10], server.FTPUser, server.FTPPass);
        }

        public override void Download()
        {
            try {
                Client.Connect();
            } catch (Exception e) {
                /*ChannelLog("Connection Failure",
                           $"Failed to connect to the server.\n{e.Message}");*/
                return;
            }

            Directory.CreateDirectory(LocalPath);

            SftpFile fileDemo = GetFile(FtpPath, DemoName);
            DownloadFile(fileDemo, $"{LocalPath}\\{fileDemo.Name}");

            SftpFile fileBsp =
                GetFile($"{FtpPath}/maps/workshop/{WorkshopId}", ".bsp");
            DownloadFile(fileBsp, $"{LocalPath}\\{fileBsp.Name}");

            Client.Disconnect();
            /*ChannelLog("Listing of Download Directory",
                       $"{string.Join("\n", Directory.GetFiles(LocalPath))}");*/
        }

        private void DownloadFile(SftpFile file, string destPath)
        {
            if (file == null) {
                /*ChannelLog("File Not Found",
                           "Failed to find the file on the server.");*/
                return;
            }

            /*ChannelLog("Downloading File From Playtest",
                       $"{file.FullName}\n{destPath}");*/

            try {
                using (Stream stream = File.OpenWrite(destPath))
                    Client.DownloadFile(file.FullName, stream);

                /*ChannelLog("Download Completed",
                           "Successfully downloaded the demo file.");*/
            } catch (Exception e) {
                /*ChannelLog("Download Failed",
                           $"Failed to download the file.\n{e.Message}");*/
            }
        }

        private SftpFile GetFile(string path, string name)
        {
            return Client.ListDirectory(path)
                         .FirstOrDefault(f => f.Name.ToLower()
                                               .Contains(name.ToLower()));
        }
    }
}
