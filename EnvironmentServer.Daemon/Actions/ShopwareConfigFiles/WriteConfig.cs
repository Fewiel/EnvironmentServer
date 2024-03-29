﻿using EnvironmentServer.DAL;
using EnvironmentServer.Util;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions.ShopwareConfigFiles;

public class WriteConfig : ActionBase
{
    public override string ActionIdentifier => "write_config";

    public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
    {
        var db = sp.GetService<Database>();
        var env = db.Environments.Get(variableID);
        var usr = db.Users.GetByID(env.UserID);
        var path = $"/home/{usr.Username}/files/{env.InternalName}/";

        var swConf = await db.ShopwareConfig.GetByEnvIDAsync(variableID);

        if (File.Exists(path + "config.php"))
        {
            File.WriteAllText(path + "config.php", swConf.Content);
            await Bash.ChownAsync(usr.Username, "sftp_users", path, true);
        }
        else if (File.Exists(path + ".env.local"))
        {
            File.WriteAllText(path + ".env.local", swConf.Content);
            await Bash.ChownAsync(usr.Username, "sftp_users", path, true);
        }
        else if (File.Exists(path + ".env"))
        {
            File.WriteAllText(path + ".env", swConf.Content);
            await Bash.ChownAsync(usr.Username, "sftp_users", path, true);
        }
        else
        {
            db.Environments.SetTaskRunning(env.ID, false);
            db.Logs.Add("ShopwareConfig", "Error - Could not find config file");
            return;
        }
        
        db.Environments.SetTaskRunning(env.ID, false);
        await db.ShopwareConfig.UpdateAsync(swConf);
    }
}