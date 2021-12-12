using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.ScheduleActions;

internal class ClearLogs : ScheduledActionBase
{
    public ClearLogs(ServiceProvider sp) : base(sp)
    {
    }

    public override string ActionIdentifier => "clear_logs";

    public override Task ExecuteAsync(Database db)
    {
        db.Logs.DeleteOld();
        return Task.CompletedTask;
    }
}
