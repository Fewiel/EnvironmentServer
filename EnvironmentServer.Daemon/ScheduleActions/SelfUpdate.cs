using CliWrap;
using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.ScheduleActions;

internal class SelfUpdate : ScheduledActionBase
{
    public SelfUpdate(ServiceProvider sp) : base(sp) { }

    public override string ActionIdentifier => "self_update";

    public override async Task ExecuteAsync(Database db)
    {
        await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"bash {db.Settings.Get("self_update_path").Value}\"")
                .ExecuteAsync();
    }
}
