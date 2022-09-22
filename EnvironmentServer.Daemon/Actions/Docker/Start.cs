using Ductus.FluentDocker.Services;
using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions.Docker;

public class Start : ActionBase
{
    public override string ActionIdentifier => "docker.start";

    public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
    {
        var hosts = new Hosts().Discover();
        var _docker = hosts.FirstOrDefault(x => x.IsNative) ?? hosts.FirstOrDefault(x => x.Name == "default");
        var db = sp.GetService<Database>();

        var container = await db.DockerContainer.GetByIDAsync(variableID);

        if (container.Active)
            return;

        foreach (var c in _docker.GetContainers())
        {
            if (c.Id == container.DockerID)
            {
                c.Start();
                container.Active = true;
                await db.DockerContainer.UpdateAsync(container);
                return;
            }
        }
    }
}