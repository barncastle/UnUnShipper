using System;
using System.IO;

namespace UnUnShipper
{
    public static class Helpers
    {
        public static bool HasAnyWildcard(string value, params string[] wildcards)
        {
            return Array.Exists(wildcards, mask => HasWildcard(value, mask));
        }

        public static bool HasWildcard(string value, string wildcard)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            var parts = wildcard.Split('*');
            var offset = 0;

            if (value[0] != '*' && !value.StartsWith(parts[0]))
                return false;

            for (var i = 0; i < parts.Length && offset != -1; i++)
                offset = value.IndexOf(parts[i], offset);

            if (value[^1] != '*' && !value.EndsWith(parts[^1]))
                return false;

            return offset != -1;
        }

        public static string GetCDNUrl(string filename, string folder)
        {
            return string.Join("/", "tpr", "wow", folder, filename.Substring(0, 2), filename.Substring(2, 2), filename);
        }

        public static string GetTempPath(string filename)
        {
            var dir = Path.Combine(Consts.TempDir, "data", filename.Substring(0, 2), filename.Substring(2, 2));
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, filename);
        }

        public static string GetProductType(string executable)
        {
            var product = Path.GetFileNameWithoutExtension(executable).ToLower();

            return product switch
            {
                "wowb" => "wow_beta",
                "wowclassic" => "wow_classic",
                "wowclassict" => "wow_classic_ptr",
                "wowclassicb" => "wow_classic_beta",
                _ => product,
            };
        }
    }
}
