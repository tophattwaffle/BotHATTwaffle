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
        private static void Main(string[] args) => new Program().StartAsync().GetAwaiter().GetResult();

        public async Task StartAsync()
        {
            while (true)
            {
                await Task.Delay(50);
                Console.Write("CMD: ");
                string cmd = Console.ReadLine();

                string address = "https://steamcommunity.com/sharedfiles/filedetails/?id=906279156";
                var result = Regex.Match(address, @"\d+$").Value;
                Console.WriteLine(result);
            }
        }
    }
}
