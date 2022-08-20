using Ductus.FluentDocker.Services;
using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions.Docker;

public class StopAll : ActionBase
{
    public override string ActionIdentifier => "docker.stopall";

    public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
    {
        var hosts = new Hosts().Discover();
        var _docker = hosts.FirstOrDefault(x => x.IsNative) ?? hosts.FirstOrDefault(x => x.Name == "default");
        var db = sp.GetService<Database>();

        foreach (var c in _docker.GetContainers())
        {
            c.Stop();
            var con = await db.DockerContainer.GetByDockerIDAsync(c.Name);
            con.Active = false;
            await db.DockerContainer.UpdateAsync(con);
            return;
        }
    }
}