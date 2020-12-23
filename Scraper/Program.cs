using System;

namespace Scraper
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            await new Mitre10Scraper().Scrape();

        }
    }
}
