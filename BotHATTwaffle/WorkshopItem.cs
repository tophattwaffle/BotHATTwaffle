using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Summer
{
    class WorkshopItem
    {
        static WebClient client = new WebClient();

        public enum ItemType
        {
            Mod,
            Collection,
            Other,
        }

        public WorkshopItem()
        {
            IsValid = false;
        }

        private static string FormatLineBreaks(string html)
        {
            html = html.Replace("<br>", "\n");

            Regex rx = new Regex("<[^>]*>");

            html = rx.Replace(html, "");

            return html;
        }

        public async Task Load(string url)
        {
            IsValid = false;

            url = url.Trim().ToLower();
            if (!url.Contains("://steamcommunity.com/sharedfiles/filedetails/") && !url.Contains("://steamcommunity.com/workshop/filedetails/"))
                return;

            Url = url;

            HtmlWeb web = new HtmlWeb();
            var htmlDoc = web.Load(Url);

            if (htmlDoc == null)
                return;

            Title = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='workshopItemTitle']")?.InnerHtml;

            Tags = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='rightDetailsBlock']")?.Descendants("a")?.Select(x => x.InnerHtml).ToArray();

            Singleplayer = false;
            foreach (var tag in Tags)
            {
                if (tag == "Singleplayer")
                    Singleplayer = true;

                if (tag == "Cooperative")
                    Singleplayer = false;
            }

            Description = FormatLineBreaks(htmlDoc.DocumentNode.SelectSingleNode("//div[@class='workshopItemDescription']")?.InnerHtml);

            Type = ItemType.Mod;

            if (htmlDoc.DocumentNode.SelectSingleNode("//div[@class='collectionHeader']") != null)
                Type = ItemType.Collection;

            if (htmlDoc.DocumentNode.SelectSingleNode("//div[@class='subscribeOption']") != null)
                Type = ItemType.Other;

            Image = htmlDoc.DocumentNode.Descendants("img")?.FirstOrDefault(d => d.Id == "previewImage")?.Attributes?.Single(x => x.Name == "src")?.Value;
            if (Image == null)
                Image = htmlDoc.DocumentNode.Descendants("img")?.FirstOrDefault(d => d.Id == "previewImageMain")?.Attributes?.Single(x => x.Name == "src")?.Value;

            AppId = 0;

            string shareclick = htmlDoc.DocumentNode.Descendants("span")?.FirstOrDefault(d => d.Id == "ShareItemBtn")?.Attributes["onclick"]?.Value;
            if (shareclick != null)
            {
                int startAppId = shareclick.IndexOf(", '") + 3;
                int endAppId = shareclick.LastIndexOf("'");

                if (startAppId != -1 && endAppId != -1)
                    int.TryParse(shareclick.Substring(startAppId, endAppId - startAppId), out AppId);
            }

            if (AppId != 0)
            {
                WebClient client = new WebClient();
                string appData = await client.DownloadStringTaskAsync(new Uri("http://store.steampowered.com/api/appdetails?appids=" + AppId));
                int index = appData.IndexOf("name");
                if (index != -1)
                {
                    appData = appData.Substring(index + 7);
                    index = appData.IndexOf("\"");
                    if (index != -1)
                        AppName = appData.Substring(0, index);
                }
            }

            AuthorName = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='friendBlockContent']")?.InnerText?.TrimStart();


            if (AuthorName.IndexOf("\r\n") != -1)
                AuthorName = AuthorName.Substring(0, AuthorName.IndexOf("\r\n"));
            if (AuthorName.IndexOf("\r") != -1)
                AuthorName = AuthorName.Substring(0, AuthorName.IndexOf("\r"));
            if (AuthorName.IndexOf("\n") != -1)
                AuthorName = AuthorName.Substring(0, AuthorName.IndexOf("\n"));

            AuthorUrl = htmlDoc.DocumentNode.SelectSingleNode("//a[@class='friendBlockLinkOverlay']")?.Attributes["href"]?.Value;
            AuthorImageUrl = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='creatorsBlock']")?.Descendants("div")?.First()?.Descendants("div")?.FirstOrDefault(x => x.HasClass("playerAvatar"))?.Descendants("img")?.First()?.Attributes["src"].Value;

            IsValid = true;
        }

        public bool Singleplayer;
        public string Url;
        public bool IsValid;
        public string Title;
        public string Description;
        public string[] Tags;
        public string Image;
        public int AppId;
        public string AppName;
        public string AuthorUrl;
        public string AuthorImageUrl;
        public string AuthorName;
        public ItemType Type;
    }
}
