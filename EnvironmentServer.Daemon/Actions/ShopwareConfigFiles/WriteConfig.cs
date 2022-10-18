using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;

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
            File.WriteAllText(swConf.Content, path + "config.php");
        }
        else if (File.Exists(path + ".env"))
        {
            File.WriteAllText(swConf.Content, path + ".env");
        }
        else
        {
            db.Environments.SetTaskRunning(env.ID, false);
            db.Logs.Add("ShopwareConfig", "Error - Could not find config file");
            return;
        }

        await db.ShopwareConfig.UpdateAsync(swConf);
        db.Environments.SetTaskRunning(env.ID, false);
    }
}