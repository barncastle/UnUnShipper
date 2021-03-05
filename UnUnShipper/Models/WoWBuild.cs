using System;
using TACT.Net.Configs;

namespace UnUnShipper.Models
{
    public class WoWBuild
    {
        public string Description { get; set; }
        public DateTime CompiledAt { get; set; }
        public KeyValueConfig PatchConfig { get; set; }
    }
}
