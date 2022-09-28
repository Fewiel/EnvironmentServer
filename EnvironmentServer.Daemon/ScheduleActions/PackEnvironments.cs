using EnvironmentServer.Daemon.Utility;
using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.ScheduleActions;

internal class PackEnvironments : ScheduledActionBase
{
    public PackEnvironments(ServiceProvider sp) : base(sp)
    {
    }

    public override string ActionIdentifier => "pack_environments";

    public override async Task ExecuteAsync(Database db)
    {
        var environments = db.Environments.GetAllUnstored();

        foreach (var env in environments)
        {
            if (env.Permanent)
                continue;

            var usr = db.Users.GetByID(env.UserID);
            var storeTime = db.Limit.GetLimit(usr, "enviroment_storetime");
            if (storeTime == 0)
                continue;

            try
            {
                if (env.LatestUse.AddDays(storeTime) < DateTime.Now && !env.Stored)
                {

                    db.Logs.Add("Daemon", $"Packing Environment: {env.InternalName} User: {db.Users.GetByID(env.UserID).Username}");

                    await EnvironmentPacker.PackEnvironmentAsync(db, env);

                    db.Logs.Add("Daemon", $"Packing complete for Environment: {env.InternalName} User: {db.Users.GetByID(env.UserID).Username}");
                }
            }
            catch (Exception ex)
            {
                db.Logs.Add("PackEnvironments", ex.ToString());
            }
        }
    }
}
