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
using System.IO;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace TestHATTwaffle
{
    class Program
    {
        private static void Main(string[] args)
        {
            string user = "TopHATTwaffle#1679";

            var split = user.Split('#');

            Console.WriteLine(split[0]);
            Console.WriteLine(split[1]);

            Console.ReadLine();
        }

    }
}
