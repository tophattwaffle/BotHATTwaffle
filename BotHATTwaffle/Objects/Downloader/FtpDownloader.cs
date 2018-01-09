using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using BotHATTwaffle.Modules.Json;
using FluentFTP;

namespace BotHATTwaffle.Objects.Downloader
{
    public sealed class FtpDownloader : Downloader<FtpClient>
    {
        private string Gamemode { get; }

        public FtpDownloader(IReadOnlyList<string> testInfo,
                             JsonServer server,
                             string demoPath) :
            base(testInfo, server, demoPath)
        {
            Gamemode = testInfo[7];
            Client =
                new FtpClient(server.Address, server.FTPUser, server.FTPPass);

            if (server.FTPType == "ftps")
            {
                Client.EncryptionMode = FtpEncryptionMode.Explicit;
                Client.SslProtocols = SslProtocols.Tls;

                Client.ValidateCertificate +=
                    (control, e) => { e.Accept = true; };
            }
        }

        public override void Download()
        {
            Client.Connect();

            // Downloads the demo file.
            DownloadFile(GetFile(FtpPath, DemoName),
                         $"{LocalPath}\\{DemoName}_{Gamemode}.dem");

            // Downloads the BSP file.
            string sourceBsp =
                GetFile($"{FtpPath}/maps/workshop/{WorkshopId}", ".bsp");
            DownloadFile(sourceBsp,
                         $"{LocalPath}\\{Path.GetFileName(sourceBsp)}");

            Client.Disconnect();
            /*ChannelLog("Listing of Download Directory",
                       $"{string.Join("\n", Directory.GetFiles(LocalPath))}");*/
        }

        private void DownloadFile(string sourcePath, string destPath)
        {
            if (sourcePath == null)
            {
                /*ChannelLog("File Not Found",
                           "Failed to find the file on the server.");*/
                return;
            }

            /*ChannelLog("Downloading File From Playtest",
                       $"{sourcePath}\n{destPath}");*/

            try {
                if (!Client.DownloadFile(destPath, sourcePath)) {
                    /*ChannelLog("Download Failed",
                               "Failed to download the file.");*/
                    return;
                }
            } catch (Exception e)
            {
                /*ChannelLog("Download Failed",
                           $"Failed to download the file.\n{e.Message}");*/
                return;
            }

            /*ChannelLog("Download Completed",
                       "Successfully downloaded the demo file.");*/
        }

        private string GetFile(string path, string name)
        {
            // If default, null is returned because string is a reference type.
            return Client.GetNameListing(path)
                         .FirstOrDefault(f => f.ToLower()
                                               .Contains(name.ToLower()));
        }
    }
}
