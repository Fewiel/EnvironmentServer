using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.ScheduleActions;

internal class LockInactiveAccounts : ScheduledActionBase
{
    public LockInactiveAccounts(ServiceProvider sp) : base(sp)
    {
    }

    public override string ActionIdentifier => "lock_inactive_accounts";

    public override Task ExecuteAsync(Database db)
    {
        //acc_cleanup_days
        foreach (var usr in db.Users.GetUsers())
        {
            if (usr.LastUsed.AddDays(int.Parse(db.Settings.Get("acc_cleanup_days").Value)) < DateTime.Now && usr.Active)
            {
                db.Logs.Add("Deamon", "Inactive account locked: " + usr.Username);
                db.Users.ChangeActiveState(usr, false);
            }
        }
        return Task.CompletedTask;
    }
}
