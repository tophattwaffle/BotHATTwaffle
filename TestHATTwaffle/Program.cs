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

namespace TestHATTwaffle
{
    class Program
    {
        public static void Main(string[] args) => new Program().StartAsync().GetAwaiter().GetResult();


        public async Task StartAsync()
        {
            Task fireAndForget = Task.Run(() =>Test());
            string site = "https://www.youtube.com/watch?v=47HR2jewQms";

            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument htmlDocument = htmlWeb.Load(site);

            Console.WriteLine("Title: " + GetYouTubeImage(site));
            await Task.Delay(1000);
            await fireAndForget;
            Console.WriteLine("Program done");
            Console.ReadLine();
        }

        public static Task Test()
        {
            Console.WriteLine("TEST WORK STATED");
            Thread.Sleep(5000);
            Console.WriteLine("TEST WORK COMPLETTE");
            return Task.CompletedTask;
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
