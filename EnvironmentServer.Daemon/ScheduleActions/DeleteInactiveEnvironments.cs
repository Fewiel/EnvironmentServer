using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.ScheduleActions;

internal class DeleteInactiveEnvironments : ScheduledActionBase
{
    public DeleteInactiveEnvironments(ServiceProvider sp) : base(sp)
    {
    }

    public override string ActionIdentifier => "delete_inactive_environments";

    public override async Task ExecuteAsync(Database db)
    {
        var environments = db.Environments.GetAll();

        foreach (var env in environments)
        {
            if (env.Permanent)
                continue;

            var usr = db.Users.GetByID(env.UserID);
            var deleteTime = db.Limit.GetLimit(usr, "enviroment_deletetime");
            if (deleteTime == 0)
                continue;

            try
            {
                if (env.LatestUse.AddDays(deleteTime) < DateTime.Now && env.Stored)
                {
                    db.Logs.Add("Daemon", $"Delete Inactive Environment: {env.InternalName} User: {db.Users.GetByID(env.UserID).Username}");

                    await db.Environments.DeleteAsync(env, usr);

                    db.Logs.Add("Daemon", $"Delete complete for Environment: {env.InternalName} User: {db.Users.GetByID(env.UserID).Username}");
                }
            }
            catch (Exception ex)
            {
                db.Logs.Add("DeleteInactiveEnvironments", ex.ToString());
            }
        }
    }
}