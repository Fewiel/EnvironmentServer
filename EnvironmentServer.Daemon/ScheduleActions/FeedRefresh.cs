using EnvironmentServer.DAL;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using Newtonsoft.Json;
using EnvironmentServer.DAL.Models;
using System.Xml.Serialization;
using EnvironmentServer.Daemon.Models;

namespace EnvironmentServer.Daemon.ScheduleActions;

internal class FeedRefresh : ScheduledActionBase
{
    public FeedRefresh(ServiceProvider sp) : base(sp) { }

    public override string ActionIdentifier => "feed_refresh";

    public override async Task ExecuteAsync(Database db)
    {
        db.ShopwareVersionInfos.Clear();
        await GatherFeedAsync(db.Settings.Get("release_feed_url_sw5").Value, db);
        await GatherFeedAsync(db.Settings.Get("release_feed_url_sw6").Value, db);
    }

    private async Task GatherFeedAsync(string url, Database db)
    {
        var client = SP.GetService<HttpClient>();
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync();
        var serializer = new XmlSerializer(typeof(ShopwareReleaseFeed));
        var result = serializer.Deserialize(stream) as ShopwareReleaseFeed;

        foreach (var r in result.Releases)
        {
            if (string.IsNullOrEmpty(r.DownloadLinkInstall))
                continue;

            var vt = r.VersionText;
            if (string.IsNullOrEmpty(vt) && r.Rc > 0)
                vt = "RC" + r.Rc;

            db.ShopwareVersionInfos.Create(new ShopwareVersionInfo
            {
                Version = r.Version,
                VersionText = vt,
                MinimumVersion = r.MinimumVersion,
                ImportantChanges = r.Locales.En.ImportantChanges,
                Changelog = r.Locales.En.Changelog,
                Type = r.Type,
                Public = r.Public,
                DownloadLinkInstall = r.DownloadLinkInstall.Trim(),
                DownloadLinkUpdate = r.DownloadLinkUpdate.Trim()
            });
        }
    }
}
