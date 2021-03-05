using System;

namespace UnUnShipper.Models
{
    public class UnshippedBuild
    {
        public string Encoding { get; set; }
        public string Install { get; set; }
        public string Download { get; set; }
        public string Size { get; set; }
        public string Root { get; set; }
        public string Build { get; set; }
        public string Product { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Encoding) &&
                !string.IsNullOrEmpty(Install) &&
                !string.IsNullOrEmpty(Download) &&
                !string.IsNullOrEmpty(Size);
        }

        public string GetDirectoryName()
        {
            if (!string.IsNullOrEmpty(Build))
                return Build + "_" + Product;

            return "Unknown_" + Encoding;
        }

        public override bool Equals(object obj)
        {
            return obj is UnshippedBuild build &&
                build.Encoding == Encoding &&
                build.Install == Install &&
                build.Download == Download &&
                build.Size == Size &&
                build.Root == Root &&
                build.Build == Build;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Encoding, 
                Install, 
                Download,
                Size,
                Root,
                Build);
        }
    }
}
