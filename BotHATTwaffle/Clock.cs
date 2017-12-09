using System;
using System.Collections.Generic;
using System.Threading;
using Discord.WebSocket;
using Discord;
using System.Threading.Tasks;

namespace BotHATTwaffle
{
    public class Clock
    {
        GoogleCalendar googleCalendar;
        bool shutDown = false;
        Thread tick;
        int clockDelay = 10000;
        int calUpdateTicks = 0;
        int calUpdateCounter = 0;

        public Clock(Dictionary<string, string> config)
        {
            if ((config.ContainsKey("clockDelay") && !int.TryParse(config["clockDelay"], out clockDelay)))
            {
                Console.WriteLine($"Key \"clockDelay\" not found or valid. Using default {clockDelay}.");
            }
            if ((config.ContainsKey("calUpdateTicks") && !int.TryParse(config["calUpdateTicks"], out calUpdateTicks)))
            {
                Console.WriteLine($"Key \"calUpdateTicks\" not found or valid. Using default {calUpdateTicks}.");
            }

            tick = new Thread(Tock);
        }

        void Tock()
        {
            while (!shutDown)
            {
                var start = DateTime.Now;
                //TODO: Loop over status effects, and see if any are expired, if so, reverse them and remove from list
                Console.WriteLine("Tick " + DateTime.Now);
                var end = DateTime.Now;
                var elapsedTime = end - start;
                if (elapsedTime.TotalMilliseconds < clockDelay)
                    Thread.Sleep(clockDelay - (int)elapsedTime.TotalMilliseconds);

                //Cal update ticks are how many times the timer will tick before checking for a calendar update.
                //If the clock delay is 5000, and cal ticks are 3, the clock will have to cycle 3 times to fire the cal update.

                if(calUpdateCounter > calUpdateTicks)
                {
                    PostPlaytestInformationAsync();
                    calUpdateCounter = 0;
                }
                else
                {
                    calUpdateCounter++;
                }
            }
        }

        public void Start()
        {
            Console.WriteLine($"Starting user timer with delay of {clockDelay}");
            tick.Start();
        }

        public void RequestFinish()
        {
            shutDown = true;
        }

        public void Finish()
        {
            shutDown = true;
            tick.Join();
        }

        
    }
}