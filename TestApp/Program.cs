using System;
using System.Collections.Generic;
using System.IO;
using WebGrabber;

namespace TestApp
{
    class Program
    {
        static void Main()
        {
            //var dbSrf = new DbSrf();
            //var json = File.ReadAllText(@"C:\Web\Grabber\download\test.json");
            //dbSrf.Insert(json);
            //Console.WriteLine("Hello World!");

            var dbSrf = new DbSrf();

            dbSrf.Import();

            var entries = new List<string>
            {
                "ChannelName;Song;Artist;PlayedDate;Messages"
            };

            foreach(var importInfo in dbSrf.ImportLog)
            {
                entries.Add(
                    $"\"{importInfo.ChannelName}\";\"{importInfo.Song}\";\"{importInfo.Artist}\";\"{importInfo.PlayedDate}\";\"{String.Join(" --- ", importInfo.Messages)}\""
                );
            }

            File.WriteAllLines(@"C:\Web\Grabber\download\logs\import_" + DateTime.Now.Ticks + ".csv", entries);

            Console.WriteLine("Hello World2!");
        }
    }
}
