using EnvironmentServer.Daemon.Models;
using EnvironmentServer.Daemon.Utility;
using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Interfaces;
using EnvironmentServer.Util;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions;

public class ExtensionTestingInstall : ActionBase
{
    public override string ActionIdentifier => "extension_testing_install";

    public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
    {
        var db = sp.GetService<Database>();
        var em = sp.GetService<IExternalMessaging>();
        var user = db.Users.GetByID(userID);
        var env = db.Environments.Get(variableID);
        var homeDir = $"/home/{user.Username}/files/{env.InternalName}";

        var version = File.ReadAllText($"/home/{user.Username}/files/{env.InternalName}/version.txt");
        var zipPath = Directory.GetFiles(homeDir, "*.zip")[0];

        if (version.StartsWith('6'))
        {
            var swVersion = new ShopwareVersion(version);
            switch (swVersion.Major)
            {
                case 4:
                    await EnvironmentPacker.DeployTemplateAsync(db, env, 6400);
                    await Bash.CommandAsync($"unzip -o -q {zipPath}", Path.Combine(homeDir, "custom/plugins/"), validation: false);
                    await Bash.CommandAsync($"composer require -W \"shopware/core:{version}\" \"shopware/administration:{version}\" \"shopware/elasticsearch:{version}\" \"shopware/storefront:{version}\" \"shopware/recovery:{version}\"", validation: false);
                    File.Move(zipPath, Path.Combine(homeDir, "custom/plugins/"));
                    break;
                case 5:
                    await EnvironmentPacker.DeployTemplateAsync(db, env, 6500);
                    await Bash.CommandAsync($"unzip -o -q {zipPath}", Path.Combine(homeDir, "custom/plugins/"), validation: false);
                    await Bash.CommandAsync($"composer require -W \"shopware/core:{version}\" \"shopware/administration:{version}\" \"shopware/elasticsearch:{version}\" \"shopware/storefront:{version}\"", validation: false);
                    File.Move(zipPath, Path.Combine(homeDir, "custom/plugins/"));
                    break;
            }
        }
        else
        {
            await EnvironmentPacker.DeployTemplateAsync(db, env, 5000);
            await Bash.CommandAsync($"unzip -o -q {zipPath}", Path.Combine(homeDir, "custom/plugins/"), validation: false);
        }

        await Bash.CommandAsync($"php bin/console user:change-password admin -p {env.DBPassword}", homeDir);
        await Bash.ChownAsync(user.Username, "sftp_users", homeDir, true);

        db.Environments.SetTaskRunning(env.ID, false);
    }
}
