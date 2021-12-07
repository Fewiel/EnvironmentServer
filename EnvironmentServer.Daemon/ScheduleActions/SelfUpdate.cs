using CliWrap;
using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.ScheduleActions;

internal class SelfUpdate : ScheduledActionBase
{
    public SelfUpdate(ServiceProvider sp) : base(sp) { }

    public override string ActionIdentifier => "self_update";

    public override async Task ExecuteAsync(Database db)
    {
        //await Cli.Wrap("/bin/bash")
        //        .WithArguments($"-c \"bash {db.Settings.Get("self_update_path").Value} | at now\"")
        //        .ExecuteAsync();
        ProcessStartInfo info = new ProcessStartInfo
        {
            Arguments = $"-c \"bash {db.Settings.Get("self_update_path").Value} | at now\"",
            CreateNoWindow = true,
            FileName = "/bin/bash",
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        Process.Start(info);
    }
}
