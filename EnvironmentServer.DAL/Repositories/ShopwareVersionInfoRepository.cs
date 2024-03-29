﻿using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using System.Collections.Generic;

namespace EnvironmentServer.DAL.Repositories
{
    public class ShopwareVersionInfoRepository
    {
        private Database DB;

        public ShopwareVersionInfoRepository(Database db)
        {
            DB = db;
        }
        //INSERT INTO `shopware_release_feed` (`id`, `version`, `version_text`, `minimum_version`, `public`, `changelog`, `important_changes`, `type`, `download_link_install`, `download_link_update`) VALUES (NULL, '6.0.0.0', '6.0.0.0', '6.0.0.0', '1', '165196165169g6h5j1gdf6hj51dgf6hj1d65h1jk6dgh1jk6d5tgh1jk6dfg5', 'd4fsh1g56yf1g6yd0fgh63sxd10h6', 'DINGSBUMBS', 'basikdnoa.zip', 'sduhbfpiqwazubdsgpi.zip');
        public void Create(ShopwareVersionInfo swversion)
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);

            c.Connection.Execute("INSERT INTO `shopware_release_feed` " +
                "(`version`, `version_text`, `minimum_version`, `public`, `changelog`, " +
                "`important_changes`, `type`, `download_link_install`, `download_link_update`) " +
                "VALUES (@version, @version_text, @minimum_version, @ispublic, @changelog, " +
                "@important_changes, @type, @download_link_install, @download_link_update);",
                new
                {
                    version = swversion.Version,
                    version_text = swversion.VersionText,
                    minimum_version = swversion.MinimumVersion,
                    ispublic = swversion.Public,
                    changelog = swversion.Changelog,
                    important_changes = swversion.ImportantChanges,
                    type = swversion.Type,
                    download_link_install = swversion.DownloadLinkInstall,
                    download_link_update = swversion.DownloadLinkUpdate
                });
        }

        public void Clear()
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            c.Connection.Execute("TRUNCATE TABLE `shopware_release_feed`");
            c.Connection.Execute("FLUSH TABLE `shopware_release_feed`");
        }

        public IEnumerable<ShopwareVersionInfo> Get()
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            return c.Connection.Query<ShopwareVersionInfo>("select * from shopware_release_feed;");
        }

        public IEnumerable<ShopwareVersionInfo> GetForMajor(int v)
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            return c.Connection.Query<ShopwareVersionInfo>("SELECT * FROM `shopware_release_feed` where LEFT(`version`, 1) = @version;",
                new
                {
                    version = v
                });
        }
    }
}