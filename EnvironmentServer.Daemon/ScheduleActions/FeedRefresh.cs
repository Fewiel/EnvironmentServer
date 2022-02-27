using EnvironmentServer.DAL;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using Newtonsoft.Json;
using EnvironmentServer.DAL.Models;
using System.Xml.Serialization;
using EnvironmentServer.Daemon.Models;
using System;

namespace EnvironmentServer.Daemon.ScheduleActions;

internal class FeedRefresh : ScheduledActionBase
{
    public FeedRefresh(ServiceProvider sp) : base(sp) { }
    private IEnumerable<ShopwareVersionInfo> TmpSW5 = new List<ShopwareVersionInfo>();
    private IEnumerable<ShopwareVersionInfo> TmpSW6 = new List<ShopwareVersionInfo>();
    public override string ActionIdentifier => "feed_refresh";

    public override async Task ExecuteAsync(Database db)
    {
        TmpSW5 = db.ShopwareVersionInfos.GetForMajor(5);
        TmpSW6 = db.ShopwareVersionInfos.GetForMajor(6);

        db.ShopwareVersionInfos.Clear();

        await GatherFeedAsync(db.Settings.Get("release_feed_url_sw5").Value, db, 5);
        await GatherFeedAsync(db.Settings.Get("release_feed_url_sw6").Value, db, 6);
    }

    private async Task GatherFeedAsync(string url, Database db, int major)
    {
        try
        {
            var client = SP.GetService<HttpClient>();
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync();
            var serializer = new XmlSerializer(typeof(ShopwareReleaseFeed));
            var result = serializer.Deserialize(stream) as ShopwareReleaseFeed;

            var tmp_list = new List<ShopwareVersionInfo>();

            foreach (var r in result.Releases)
            {
                if (string.IsNullOrEmpty(r.DownloadLinkInstall))
                    continue;

                var vt = r.VersionText;
                if (string.IsNullOrEmpty(vt) && r.Rc > 0)
                    vt = "RC" + r.Rc;

                tmp_list.Add(new ShopwareVersionInfo
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

            if (tmp_list.Count == 0)
            {
                db.Logs.Add("Web", "Error FeedRefresh, restore Backup. List == 0");

                if (major == 5)
                {
                    foreach (var v in TmpSW5)
                    {
                        db.ShopwareVersionInfos.Create(v);
                    }
                }
                else
                {
                    foreach (var v in TmpSW6)
                    {
                        db.ShopwareVersionInfos.Create(v);
                    }
                }
            }
            else
            {
                foreach (var v in tmp_list)
                {
                    db.ShopwareVersionInfos.Create(v);
                }
                db.Logs.Add("Web", "FeedRefresh Sucessful");
            }
        }
        catch (Exception ex)
        {
            db.Logs.Add("Web", "Error FeedRefresh, restore Backup. " + ex.Message);

            if (major == 5)
            {
                foreach (var v in TmpSW5)
                {
                    db.ShopwareVersionInfos.Create(v);
                }
            }
            else
            {
                foreach (var v in TmpSW6)
                {
                    db.ShopwareVersionInfos.Create(v);
                }
            }
        }
    }
}