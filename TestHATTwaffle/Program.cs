/*
 * THIS IS USED ONLY FOR TESTING
 */
using CoreRCON;
using CoreRCON.Parsers.Standard;
using FluentFTP;
using System;
using System.Net;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using HtmlAgilityPack;
using System.Linq;
using System.Collections.Generic;
using System.Web;
using System.ComponentModel;
using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;

namespace TestHATTwaffle
{
    class Program
    {
        public static void Main(string[] args)
        {
            while (true)
            {
                BackgroundWorker bgWorker = new BackgroundWorker();
                bgWorker.DoWork += (sender, e) => { Test(); };
                bgWorker.RunWorkerAsync();


                Console.ReadLine();
            }
        }

        public static void GetTitle()
        {
            string site = "https://www.youtube.com/watch?v=47HR2jewQms";

            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument htmlDocument = htmlWeb.Load(site);

            Console.WriteLine("Title: " + GetYouTubeImage(site));
        }

        async static public void Test()
        {
            string albumURL = "https://imgur.com/a/PItKx";

            string albumID = albumURL.Substring(albumURL.IndexOf("/a/") + 3);

            Console.WriteLine(albumID);

            Console.WriteLine("TEST WORK STATED");
            var client = new ImgurClient("fc19d69916d543e");
            var endpoint = new AlbumEndpoint(client);
            var album = await endpoint.GetAlbumAsync(albumID);

            Console.WriteLine("REMAIN: " + client.RateLimit.ClientRemaining);

            Random _rand = new Random();

            var tmpArray = album.Images.ToArray();

            var finalimg = tmpArray[(_rand.Next(0,tmpArray.Length))].Link;

            Console.WriteLine("TEST WORK COMPLETTE " + finalimg);
        }

        public static string GetYouTubeImage(string videoUrl)
        {
            int mInd = videoUrl.IndexOf("/watch?v=");
            if (mInd != -1)
            {
                string strVideoCode = videoUrl.Substring(videoUrl.IndexOf("/watch?v=") + 9);
                Console.WriteLine(strVideoCode);
                //int ind = strVideoCode.IndexOf("?");
                //strVideoCode = strVideoCode.Substring(0, ind == -1 ? strVideoCode.Length : ind);
                return "https://img.youtube.com/vi/" + strVideoCode + "/hqdefault.jpg";
            }
            else
                return "";
        }
    }
}
