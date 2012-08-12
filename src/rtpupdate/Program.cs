using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace rtpupdate
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Running");
            new RtpScraper(new rtpdbDataContext()).execute();
            Console.WriteLine("Done");
        }

    }
}
