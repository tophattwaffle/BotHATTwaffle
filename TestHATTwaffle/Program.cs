/*
 * THIS IS USED ONLY FOR TESTING
 */
using CoreRCON;
using CoreRCON.Parsers.Standard;
using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TestHATTwaffle
{
    class Program
    {
        private static void Main(string[] args)
        {
            string externalip = new WebClient().DownloadString("http://icanhazip.com");
            Console.WriteLine(externalip);
        }
    }
}
