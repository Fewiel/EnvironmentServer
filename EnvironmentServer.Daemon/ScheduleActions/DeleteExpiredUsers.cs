using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.ScheduleActions;

internal class DeleteExpiredUsers : ScheduledActionBase
{
    public DeleteExpiredUsers(ServiceProvider sp) : base(sp)
    {
    }

    public override string ActionIdentifier => "delete_expired_users";

    public override async Task ExecuteAsync(Database db)
    {
        foreach (var usr in db.Users.GetTempUsers())
        {
            if (usr.ExpirationDate < DateTime.Now)
            {
                await db.Users.DeleteAsync(usr);
                db.Logs.Add("Scheduler" ,"Expired User: " + usr.Username);
            }
        }
    }
}
