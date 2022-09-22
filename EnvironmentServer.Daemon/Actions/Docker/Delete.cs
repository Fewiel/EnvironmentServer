using Ductus.FluentDocker.Services;
using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions.Docker;

public class Delete : ActionBase
{
    public override string ActionIdentifier => "docker.delete";

    public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
    {
        var hosts = new Hosts().Discover();
        var _docker = hosts.FirstOrDefault(x => x.IsNative) ?? hosts.FirstOrDefault(x => x.Name == "default");
        var db = sp.GetService<Database>();

        var container = await db.DockerContainer.GetByIDAsync(variableID);

        foreach (var c in _docker.GetContainers())
        {
            if (c.Id == container.DockerID)
            {
                c.Stop();
                c.Dispose();
                c.Remove(true);
                db.DockerContainer.Delete(container);
                var filePath = $"/root/DockerFiles/{container.ID}.yml";
                if (File.Exists(filePath))
                    File.Delete(filePath);
                return;
            }
        }
    }
}