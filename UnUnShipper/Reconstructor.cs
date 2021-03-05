using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TACT.Net.BlockTable;
using TACT.Net.Encoding;
using TACT.Net.Install;
using TACT.Net.Network;
using UnUnShipper.Models;

namespace UnUnShipper
{
    public class Reconstructor
    {
        private readonly CDNClient Client;

        public Reconstructor()
        {
            Client = new CDNClient(Consts.CDNs);
        }

        public async Task AttemptReconstruction(UnshippedBuild model)
        {          
            Directory.CreateDirectory(Consts.TempDir);

            if (!await DownloadFile(model.Encoding, Helpers.GetTempPath(model.Encoding)))
                return;
            if (!await DownloadFile(model.Install, Helpers.GetTempPath(model.Install)))
                return;

            Console.WriteLine($"Processing Encoding {model.Encoding}");

            // these files are less important
            await DownloadFile(model.Download, Helpers.GetTempPath(model.Download));
            await DownloadFile(model.Size, Helpers.GetTempPath(model.Size));

            // load encoding file
            using var fs = File.OpenRead(Helpers.GetTempPath(model.Encoding));
            using var blte = new BlockTableStreamReader(fs);
            var eSize = blte.Length;
            var ecSize = fs.Length;
            var encoding = new EncodingFile(blte);
            blte.Close();
            fs.Close();

            // extract all binaries
            await ExtractFiles(model, encoding);

            // attempt to find and download root
            if (await TryGetRoot(model, encoding))
                await DownloadFile(model.Root, Helpers.GetTempPath(model.Root));

            // generate buildconfig
            ConfigGenerator.BuildConfig(model, encoding, eSize, ecSize);            

            Directory.Move(Consts.TempDir, model.GetDirectoryName());
            Console.WriteLine($"{model.Encoding} moved to {model.GetDirectoryName()}");
        }

        private async Task<bool> TryGetRoot(UnshippedBuild model, EncodingFile encoding)
        {
            // find the zlib espec only used for a few files
            // namely root and world/liquid.tex
            var espec = encoding.ESpecStringTable.IndexOf("z");

            // attemp to filter out just the root with min size
            var files = encoding.EKeyEntries
                .Where(x => x.ESpecIndex == espec && x.CompressedSize > Consts.MinRootSize)
                .ToArray();

            if (files.Length == 0)
                return false;

            // read the magic of each file
            var buffer = new byte[4];
            for (var i = 0; i < files.Length; i++)
            {
                var endpoint = Helpers.GetCDNUrl(files[i].EKey.ToString(), "data");
                var stream = await Client.OpenStream(endpoint, 0, 0x1000); // arbitary peek size
                var blte = new BlockTableStreamReader(stream);
                blte.Read(buffer);

                if (BitConverter.ToUInt32(buffer) == Consts.RootMagic)
                {
                    model.Root = files[i].EKey.ToString();
                    return true;
                }
            }

            return false;
        }

        private async Task ExtractFiles(UnshippedBuild model, EncodingFile encoding)
        {
            var install = new InstallFile(Helpers.GetTempPath(model.Install));

            foreach (var file in install.Files)
            {
                if (!Helpers.HasAnyWildcard(file.FilePath, Consts.FileWildcards))
                    continue;
                if (!encoding.TryGetCKeyEntry(file.CKey, out var contentEntry))
                    continue;
                if (contentEntry.EKeys.Count == 0)
                    continue;

                var filepath = Path.Combine(Consts.TempDir, file.FilePath);
                Directory.CreateDirectory(Path.GetDirectoryName(filepath));

                // download the first one
                foreach (var ekey in contentEntry.EKeys)
                {
                    if (await DownloadFile(ekey.ToString(), filepath))
                    {
                        // decode file, using swap temp to avoid rw conflicts
                        BlockTableEncoder.DecodeAndExport(filepath, filepath + "_temp");
                        File.Copy(filepath + "_temp", filepath, true);
                        File.Delete(filepath + "_temp");

                        if (file.FilePath.EndsWith(".exe"))
                        {
                            model.Build ??= FileVersionInfo.GetVersionInfo(filepath).FileVersion;
                            model.Product ??= Helpers.GetProductType(file.FilePath);
                        }

                        break;
                    }
                }                
            }
        }

        private async Task<bool> DownloadFile(string hash, string filename)
        {
            if (File.Exists(filename))
                return true;

            return await Client.DownloadFile(Helpers.GetCDNUrl(hash, "data"), filename);
        }
    }
}
