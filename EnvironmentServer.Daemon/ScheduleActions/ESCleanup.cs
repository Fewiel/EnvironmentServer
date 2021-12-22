using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.ScheduleActions;

internal class ESCleanup : ScheduledActionBase
{
    public ESCleanup(ServiceProvider sp) : base(sp)
    {
    }

    public override string ActionIdentifier => "es_cleanup";

    public override async Task ExecuteAsync(Database db)
    {
        await db.EnvironmentsES.StopAll();
        await db.EnvironmentsES.Cleanup();
    }
}
