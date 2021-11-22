namespace EnvironmentServer.DAL.Models;

public class ShopwareVersionInfo
{
    public string Version { get; set; }
    public string VersionText { get; set; }
    public string MinimumVersion { get; set; }
    public int Public { get; set; }
    public string Changelog { get; set; }
    public string ImportantChanges { get; set; }
    public string Type { get; set; }
    public string DownloadLinkInstall { get; set; }
    public string DownloadLinkUpdate { get; set; }
}
