using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TACT.Net.Configs;
using UnUnShipper.Models;

namespace UnUnShipper
{
    public class WoWToolsScraper
    {
        private readonly int Limit;
        private readonly WebClient Client;
        private readonly List<WoWBuild> Builds;
        private readonly HashSet<string> KnownSystemFiles;
        private readonly Regex ModalRegex = new(@"fillVersionModal\((\d+)\)");

        public WoWToolsScraper(int limit)
        {
            Limit = Math.Max(limit, 1);
            Client = new WebClient();
            Builds = new List<WoWBuild>(0x100);
            KnownSystemFiles = new HashSet<string>(0x200);
        }

        public async Task ScrapeBuilds()
        {
            Console.WriteLine("Started scraping...");

            var buildspage = await Client.DownloadStringTaskAsync(Consts.WoWToolsBuilds);
            var modals = ModalRegex.Matches(buildspage);
            if (modals == null || modals.Count == 0)
                return;

            var modalIds = modals
                .Select(x => int.Parse(x.Groups[1].Value))
                .OrderByDescending(x => x)
                .Take(Limit + 30) // hardcoded look behind limit
                .ToArray();

            var doc = new HtmlDocument();
            for (var i = 0; i < modalIds.Length; i++)
                Builds.Add(await ScrapeBuild(modalIds[i], doc));

            // sort newest to oldest
            Builds.Sort((x, y) => -x.CompiledAt.CompareTo(y.CompiledAt));
        }

        public UnshippedBuild[] GetUnshippedBuilds()
        {
            var results = new HashSet<UnshippedBuild>();
            var wanted = DateTime.Now.AddMonths(-2);

            for(var i = 0; i < Limit && i < Builds.Count; i++)
            {
                var build = Builds[i];
                var count = build.PatchConfig?.GetValues("encoding")?.Count ?? 0;

                // skip the current patch details
                for (var j = 5; j < count; j += 4)
                {
                    var model = new UnshippedBuild
                    {
                        Encoding = build.PatchConfig.GetValue("encoding", j),
                        Install = build.PatchConfig.GetValue("install", j),
                        Download = build.PatchConfig.GetValue("download", j),
                        Size = build.PatchConfig.GetValue("size", j)
                    };

                    // check atleast one file is unshipped
                    if (!KnownSystemFiles.Contains(model.Encoding) ||
                        !KnownSystemFiles.Contains(model.Install) ||
                        !KnownSystemFiles.Contains(model.Download) ||
                        !KnownSystemFiles.Contains(model.Size))
                        results.Add(model);
                }
            }

            // remove results missing files we need
            results.RemoveWhere(r => !r.IsValid());
            return results.ToArray();
        }

        private async Task<WoWBuild> ScrapeBuild(int modal, HtmlDocument doc)
        {
            var model = new WoWBuild();
            var endpoint = string.Format(Consts.WoWToolsBuildInfoApi, modal);
            var buildModal = await Client.DownloadStringTaskAsync(endpoint);
            
            doc.LoadHtml(buildModal);

            var descriptionNode = doc.DocumentNode.SelectSingleNode("/table[1]/tr[1]/td[2]");
            var compiledNode = doc.DocumentNode.SelectSingleNode("/table[1]/tr[3]/td[2]");
            var patchConfigNode = doc.DocumentNode.SelectSingleNode("/table[2]/tr[3]/td[2]");
            var systemFileNodes = doc.DocumentNode.SelectNodes("/table[3]/tr");

            if (descriptionNode != null)
            {
                model.Description = descriptionNode.InnerText;
                Console.WriteLine($"\tParsing {model.Description}");
            }

            if (compiledNode != null)
                model.CompiledAt = DateTime.Parse(compiledNode.InnerText);

            if (patchConfigNode != null)
                await ScrapePatchConfig(model, patchConfigNode.InnerText);

            if (systemFileNodes != null)
            {
                foreach (var node in systemFileNodes)
                {
                    var hash = node.SelectSingleNode(".//td[3]/span")?.InnerText;
                    if (hash != null)
                        KnownSystemFiles.Add(hash);
                }
            }

            return model;
        }

        private async Task ScrapePatchConfig(WoWBuild model, string filehash)
        {
            var endpoint = string.Format(Consts.WoWToolsConfigApi, filehash);
            var config = await Client.DownloadStringTaskAsync(endpoint);
            config = config.Replace("<pre>", "").Replace("</pre>", "");

            using var reader = new MemoryStream(Encoding.UTF8.GetBytes(config));
            model.PatchConfig = new KeyValueConfig(reader, ConfigType.PatchConfig);
        }
    }
}
