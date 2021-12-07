using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.ScheduleActions;

public abstract class ScheduledActionBase
{
    protected readonly ServiceProvider SP;

    public ScheduledActionBase(ServiceProvider sp) => SP = sp;

    public abstract string ActionIdentifier { get; }
    public abstract Task ExecuteAsync(Database db);
}
