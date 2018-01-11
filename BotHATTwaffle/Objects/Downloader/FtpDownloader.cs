using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using BotHATTwaffle.Modules.Json;
using FluentFTP;

namespace BotHATTwaffle.Objects.Downloader
{
	public sealed class FtpDownloader : Downloader<FtpClient>
	{
		private readonly string gamemode;

		public FtpDownloader(IReadOnlyList<string> testInfo, JsonServer server, DataServices dataSvc) : base(testInfo, server, dataSvc)
		{
			gamemode = testInfo[7];
			Client = new FtpClient(server.Address, server.FTPUser, server.FTPPass);

			if (server.FTPType == "ftps")
			{
				Client.EncryptionMode = FtpEncryptionMode.Explicit;
				Client.SslProtocols = SslProtocols.Tls;

				Client.ValidateCertificate += (control, e) => { e.Accept = true; };
			}
		}

		public override void Download()
		{
			try
			{
				Client.Connect();
			}
			catch (Exception e)
			{
				DataSvc.ChannelLog("Connection Failure", "Failed to connect to the server.\n" + $"{e.Message}");
				return;
			}

			// Downloads the demo file.
			DownloadFile(GetFile(FtpPath, DemoName), $"{LocalPath}\\{DemoName}_{gamemode}.dem");

			// Downloads the BSP file.
			string sourceBsp = GetFile($"{FtpPath}/maps/workshop/{WorkshopId}", ".bsp");
			DownloadFile(sourceBsp, $"{LocalPath}\\{Path.GetFileName(sourceBsp)}");

			Client.Disconnect();
			DataSvc.ChannelLog("Listing of Download Directory", $"{string.Join("\n", Directory.GetFiles(LocalPath))}");
		}

		private void DownloadFile(string sourcePath, string destPath)
		{
			if (sourcePath == null)
			{
				DataSvc.ChannelLog("File Not Found", "Failed to find the file on the server.");
				return;
			}

			DataSvc.ChannelLog("Downloading File From Playtest", $"{sourcePath}\n{destPath}");

			try
			{
				if (!Client.DownloadFile(destPath, sourcePath))
				{
					DataSvc.ChannelLog("Download Failed", "Failed to download the file.");
				}
			}
			catch (Exception e)
			{
				DataSvc.ChannelLog("Download Failed", "Failed to download the file.\n" + $"{e.Message}");
			}

			DataSvc.ChannelLog("Download Completed", "Successfully downloaded the demo file.");
		}

		private string GetFile(string path, string name)
		{
			// If default, null is returned because string is a reference type.
			return Client.GetNameListing(path).FirstOrDefault(f => f.ToLower().Contains(name.ToLower()));
		}
	}
}
