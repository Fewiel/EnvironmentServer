using System;
using System.Text.RegularExpressions;

namespace EnvironmentServer.Daemon.Models
{
    public class ShopwareVersion
    {
        private const string VersionRegex = @"(\d+)\.(\d+)\.(\d+)\.(\d+)(?:-rc(\d+))?";
        private static Regex Regex = new Regex(VersionRegex);

        public int ShopwareMain { get; }
        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }
        public int? RC { get; }

        public ShopwareVersion(string version)
        {
            if (version.ToLower().Contains("trunk"))
                return; 

            Match match = Regex.Match(version);

            if (match.Success)
            {
                ShopwareMain = int.Parse(match.Groups[1].Value);
                Major = int.Parse(match.Groups[2].Value);
                Minor = int.Parse(match.Groups[3].Value);
                Patch = int.Parse(match.Groups[4].Value);
                RC = match.Groups[5].Success ? int.Parse(match.Groups[5].Value) : null;
            }
            else
            {
                throw new ArgumentException("Invalid Shopware version format.", nameof(version));
            }
        }
    }
}