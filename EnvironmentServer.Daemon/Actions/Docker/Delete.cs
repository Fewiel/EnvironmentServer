using CliWrap;
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
        var filePath = $"/root/DockerFiles/{container.ID}.yml";
        foreach (var c in _docker.GetContainers())
        {
            if (c.Id == container.DockerID)
            {
                c.Stop();
                c.Dispose();
                c.Remove(true);

                var httpProxyPath = $"/etc/apache2/sites-avalibe/web-container-{container.ID}.conf";
                if (File.Exists(httpProxyPath))
                {                    
                    await Cli.Wrap("/bin/bash")
                    .WithArguments($"-c \"a2dissite web-container-{container.ID}.conf\"")
                    .ExecuteAsync();
                    await Cli.Wrap("/bin/bash")
                        .WithArguments("-c \"service apache2 reload\"")
                        .ExecuteAsync();
                    File.Delete(httpProxyPath);
                }

                db.DockerContainer.Delete(container);
                if (File.Exists(filePath))
                    File.Delete(filePath);
                return;
            }
        }

        db.DockerContainer.Delete(container);
        if (File.Exists(filePath))
            File.Delete(filePath);
    }
}