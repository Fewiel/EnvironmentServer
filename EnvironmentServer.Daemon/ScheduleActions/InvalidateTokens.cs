using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.ScheduleActions;

internal class InvalidateTokens : ScheduledActionBase
{
    public InvalidateTokens(ServiceProvider sp) : base(sp)
    {
    }

    public override string ActionIdentifier => "invalidate_tokens";

    public override Task ExecuteAsync(Database db)
    {
        db.Tokens.DeleteOldTokens();
        return Task.CompletedTask;
    }
}
