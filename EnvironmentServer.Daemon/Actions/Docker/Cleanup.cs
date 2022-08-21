using Ductus.FluentDocker.Services;
using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions.Docker;

public class Cleanup : ActionBase
{
    public override string ActionIdentifier => "docker.cleanup";

    public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
    {
        var hosts = new Hosts().Discover();
        var _docker = hosts.FirstOrDefault(x => x.IsNative) ?? hosts.FirstOrDefault(x => x.Name == "default");
        var db = sp.GetService<Database>();
        var containers = await db.DockerContainer.GetAllAsync();

        foreach (var c in _docker.GetContainers())
        {
            var con = containers.FirstOrDefault(container => container.Name == c.Name);
            if (con == null)
            {
                c.Remove(true);
                continue;
            }

            if (con.Active)
            {
                c.Start();
            }
            else
            {
                c.Stop();
            }
        }
    }
}