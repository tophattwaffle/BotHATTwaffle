using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BotHATTwaffle.Models;

using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace BotHATTwaffle.Services.Download
{
    public sealed class SftpDownloader : Downloader<SftpClient>
    {
        public SftpDownloader(IReadOnlyList<string> testInfo, Server server, DataService dataSvc) : base(testInfo, server,
            dataSvc)
        {
            Client = new SftpClient(testInfo[10], server.ftp_username, server.ftp_password);
        }

        public override void Download()
        {
            try
            {
                Client.Connect();
            }
            catch (Exception e)
            {
                DataSvc.ChannelLog("Connection Failure", $"Failed to connect to the server.\n{e.Message}");
                return;
            }

            Directory.CreateDirectory(LocalPath);

            SftpFile fileDemo = GetFile(FtpPath, DemoName);
            DownloadFile(fileDemo, $"{LocalPath}\\{fileDemo.Name}");

            SftpFile fileBsp = GetFile($"{FtpPath}/maps/workshop/{WorkshopId}", ".bsp");
            DownloadFile(fileBsp, $"{LocalPath}\\{fileBsp.Name}");

            Client.Disconnect();
            DataSvc.ChannelLog("Listing of Download Directory", $"{string.Join("\n", Directory.GetFiles(LocalPath))}");
        }

        private void DownloadFile(SftpFile file, string destPath)
        {
            if (file == null)
            {
                DataSvc.ChannelLog("File Not Found", "Failed to find the file on the server.");
                return;
            }

            DataSvc.ChannelLog("Downloading File From Playtest", $"{file.FullName}\n{destPath}");

            try
            {
                using (Stream stream = File.OpenWrite(destPath))
                    Client.DownloadFile(file.FullName, stream);

                DataSvc.ChannelLog("Download Completed", "Successfully downloaded the demo file.");
            }
            catch (Exception e)
            {
                DataSvc.ChannelLog("Download Failed", $"Failed to download the file.\n{e.Message}");
            }
        }

        private SftpFile GetFile(string path, string name)
        {
            return Client.ListDirectory(path).FirstOrDefault(f => f.Name.ToLower().Contains(name.ToLower()));
        }
    }
}
