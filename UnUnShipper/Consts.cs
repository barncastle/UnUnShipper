namespace UnUnShipper
{
    public static class Consts
    {
        public const string WoWToolsBuilds = "https://wow.tools/builds/";
        public const string WoWToolsBuildInfoApi = "https://wow.tools/builds/index.php?api=buildinfo&versionid={0}";
        public const string WoWToolsConfigApi = "https://wow.tools/builds/index.php?api=configdump&config={0}";

        public const int MinRootSize = 25000000;
        public const uint RootMagic = 0x4D465354;
        public const string TempDir = "temp";
        public const string DataDir = "data";

        public static readonly string[] CDNs = new[]
        {
            "level3.blizzard.com",
            "eu.cdn.blizzard.com",
            "us.cdn.blizzard.com",
            "client04.pdl.wow.battlenet.com.cn",
            "client05.pdl.wow.battlenet.com.cn",
            "client02.pdl.wow.battlenet.com.cn",
            "blizzard.nefficient.co.kr"
        };

        public static readonly string[] FileWildcards = new[]
        {
            "Wow*.exe",
            "Wow*.pdb",
            "RenderService*.exe",
            "RenderService*.pdb",
            "World of Warcraft*.app\\Contents\\MacOS\\World of Warcraft*"
        };
    }
}
