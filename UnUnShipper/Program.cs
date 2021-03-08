using System;
using System.Threading.Tasks;

namespace UnUnShipper
{
    class Program
    {
        private const int DefaultLimit = 1;

        static async Task Main(string[] args)
        {
            var scraper = GetScraper(args);
            var reconstructor = new Reconstructor();

            await scraper.ScrapeBuilds();
            var builds = scraper.GetUnshippedBuilds();

            Console.WriteLine($"Found {builds.Length} potentially unshipped builds...");
            Console.WriteLine();

            foreach (var build in builds)
                await reconstructor.AttemptReconstruction(build);
        }

        private static WoWToolsScraper GetScraper(string[] args)
        {
            int limit = DefaultLimit;
            if (args?.Length > 0 && !int.TryParse(args[0], out limit))
                limit = DefaultLimit;

            return new WoWToolsScraper(limit);
        }
    }
}
