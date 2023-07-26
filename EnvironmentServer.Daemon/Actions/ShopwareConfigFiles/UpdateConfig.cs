using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace EnvironmentServer.Daemon.Actions.ShopwareConfigFiles;

public class UpdateConfig : ActionBase
{
    public override string ActionIdentifier => "update_config";

    public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
    {
        var db = sp.GetService<Database>();
        var env = db.Environments.Get(variableID);
        var usr = db.Users.GetByID(env.UserID);
        var path = $"/home/{usr.Username}/files/{env.InternalName}/";

        var swConf = await db.ShopwareConfig.GetByEnvIDAsync(variableID);

        if (File.Exists(path + "config.php"))
        {
            swConf.Content = File.ReadAllText(path + "config.php");
        }
        else if (File.Exists(path + ".env.local"))
        {
            swConf.Content = File.ReadAllText(path + ".env.local");
        }
        else if (File.Exists(path + ".env"))
        {
            swConf.Content = File.ReadAllText(path + ".env");
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
