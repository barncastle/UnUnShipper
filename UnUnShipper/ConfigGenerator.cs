using TACT.Net.Configs;
using TACT.Net.Cryptography;
using TACT.Net.Encoding;
using UnUnShipper.Models;

namespace UnUnShipper
{
    public static class ConfigGenerator
    {
        public static void BuildConfig(UnshippedBuild model, EncodingFile encoding, long eSize, long eCompressedSize)
        {
            var config = new KeyValueConfig(ConfigType.BuildConfig);         
            SetFileInfo(config, "root", model.Root, encoding);
            SetFileInfo(config, "install", model.Install, encoding);
            SetFileInfo(config, "download", model.Download, encoding);
            SetFileInfo(config, "size", model.Size, encoding);

            config.SetValue("encoding", encoding.Checksum, 0);
            config.SetValue("encoding", model.Encoding, 1);
            config.SetValue("encoding-size", eSize, 0);
            config.SetValue("encoding-size", eCompressedSize, 1);

            config.SetValue("build-name", GetBuildName(model.Product, model.Build));
            config.SetValue("build-uid", model.Product);

            config.Write(Consts.TempDir);
        }

        private static void SetFileInfo(KeyValueConfig config, string type, string hash, EncodingFile encoding)
        {
            if (!MD5Hash.TryParse(hash, out var ekey))
                return;
            if (!encoding.TryGetEKeyEntry(ekey, out var encodedEntry))
                return;

            var contentEntry = encoding.GetCKeyEntryByEKey(ekey);
            if (contentEntry == null)
                return;

            config.SetValue(type, contentEntry.CKey, 0);

            if (type != "root")
            {
                config.SetValue(type, hash, 1);
                config.SetValue(type + "-size", contentEntry.DecompressedSize, 0);
                config.SetValue(type + "-size", encodedEntry.CompressedSize, 1);
            }
        }

        private static string GetBuildName(string product, string build)
        {
            if (string.IsNullOrEmpty(build))
                return "";

            var buildNumberIndex = build.LastIndexOf('.');
            var version = build[..buildNumberIndex];
            var buildnumber = build[(buildNumberIndex + 1)..];

            return $"WOW-{buildnumber}patch{version}_" + product switch
            {
                "wow" => "Retail",
                "wowt" => "PTR",
                "wow_beta" => "Beta",
                "wow_classic" => "ClassicRetail",
                "wow_classic_ptr" => "ClassicPTR",
                _ => "Unknown_" + product
            };
        }
    }
}
