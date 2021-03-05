using System;
using System.Threading.Tasks;

namespace UnUnShipper
{
    class Program
    {
        static async Task Main()
        {
            var scraper = new WoWToolsScraper();
            var reconstructor = new Reconstructor();

            await scraper.ScrapeBuilds();
            var builds = scraper.GetUnshippedBuilds();

            Console.WriteLine($"Found {builds.Length} potentially unshipped builds!");
            Console.WriteLine();

            foreach (var build in builds)
                await reconstructor.AttemptReconstruction(build);
        }
    }
}
