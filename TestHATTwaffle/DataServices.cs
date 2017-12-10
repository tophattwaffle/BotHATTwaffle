using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace TestHATTwaffle
{
    class DataServices
    {
        JObject searchData;
        JsonRoot root;
        List<JsonSeries> series;

        public DataServices()
        {
            Start();
        }

        private void Start()
        {
            string dataPath = "searchData.json";
            searchData = JObject.Parse(File.ReadAllText(dataPath));

            root = searchData.ToObject<JsonRoot>();
            series = root.series;
        }

        public List<JsonTutorial> Search(string searchSeries, string searchTerm)
        {
            List<JsonTutorial> results = new List<JsonTutorial>();
            var bootcamp = series[0];
            var v2 = series[1];

            var returns = v2.tutorial.FindAll(x => x.tags.Contains(searchTerm));

            foreach(var result in returns)
            {
                Console.WriteLine(result.url);
            }

            Console.WriteLine($"\n\n\n\nGetting FAQ results for the search\n");

            string faqurl = "https://www.tophattwaffle.com/wp-admin/admin-ajax.php?action=epkb-search-kb&epkb_kb_id=1&search_words=";

            HtmlWeb hw = new HtmlWeb();
            HtmlDocument doc = hw.Load($"{faqurl}{searchTerm}");
            foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
            {
                string newurl = link.GetAttributeValue("href", string.Empty).Replace(@"\", "").Replace("\"","");

                HtmlWeb hw2 = new HtmlWeb();
                HtmlDocument doc2 = hw2.Load(newurl);

                string title = (from x in doc2.DocumentNode.Descendants()
                                where x.Name.ToLower() == "title"
                                select x.InnerText).FirstOrDefault();


                string description = doc2.GetElementbyId("kb-article-content").InnerText;

                if(description.Length >= 513)
                {
                    description = description.Substring(0, 512);
                }

                List<string> imgs = (from x in doc2.DocumentNode.Descendants()
                                     where x.Name.ToLower() == "img"
                                     select x.Attributes["src"].Value).ToList<String>();

                string finalImg = imgs[0];
                if(imgs.Count > 1)
                    finalImg = imgs[2];

                Console.WriteLine(title);
                Console.WriteLine(description);
                Console.WriteLine(finalImg);
                Console.WriteLine(newurl);
            }



            //TODO: Basically everything

            //Download webpage title and store to string

            /*
            string URL = null;
            WebClient x = new WebClient();
            string siteTitle = x.DownloadString(URL);
            string regex = @"(?<=<title.*>)([\s\S]*)(?=</title>)";
            Regex ex = new Regex(regex, RegexOptions.IgnoreCase);
            siteTitle = ex.Match(siteTitle).Value.Trim();
            */
            return results;
        }
    }
}
