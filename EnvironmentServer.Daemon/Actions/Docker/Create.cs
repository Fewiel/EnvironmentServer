using Ductus.FluentDocker.Commands;
using Ductus.FluentDocker.Services;
using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using Ductus.FluentDocker.Builders;
using System.Linq;
using System.Threading.Tasks;
using EnvironmentServer.Daemon.Utility;
using System.IO;
using Ductus.FluentDocker;
using EnvironmentServer.DAL.StringConstructors;
using CliWrap;

namespace EnvironmentServer.Daemon.Actions.Docker
{
    internal class Create : ActionBase
    {
        public override string ActionIdentifier => "docker.create";

        public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
        {
            var hosts = new Hosts().Discover();
            var _docker = hosts.FirstOrDefault(x => x.IsNative) ?? hosts.FirstOrDefault(x => x.Name == "default");
            var db = sp.GetService<Database>();

            if (_docker == null)
            {
                db.Logs.Add("docker.start", "Docker not found on Host");
                return;
            }

            var container = await db.DockerContainer.GetByIDAsync(variableID);
            var fileTemplate = await db.DockerComposeFile.GetByIDAsync(container.DockerComposeFileID);
            var usedPorts = db.DockerPort.Get().Select(i => i.Port).ToList();
            var minPortSetting = db.Settings.Get("docker_port_min");
            var minPort = 10000;

            if (minPortSetting != null)
                minPort = int.Parse(minPortSetting.Value);

            var dockerFile = DockerFileBuilder.Build(fileTemplate.FileContent, usedPorts, minPort);

            foreach (var dp in dockerFile.Variables)
            {
                db.DockerPort.Insert(new() { Port = dp.Value, Name = dp.Key, DockerContainerID = container.ID });
            }

            if (dockerFile.Variables.ContainsKey("http"))
            {
                File.WriteAllText($"/etc/apache2/sites-avalibe/web-container-{container.ID}.conf", ProxyConfConstructor.Construct
                    .WithPort(dockerFile.Variables["http"])
                    .WithDomain($"web-container-{container.ID}.{db.Settings.Get("domain").Value}").BuildHttpProxy());
                await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"a2ensite web-container-{container.ID}.conf\"")
                .ExecuteAsync();
                await Cli.Wrap("/bin/bash")
                    .WithArguments("-c \"service apache2 reload\"")
                    .ExecuteAsync();
            }


            var filePath = $"/root/DockerFiles/{container.ID}.yml";

            File.WriteAllText(filePath, dockerFile.Content);

            var svc = new Builder()
                        .UseContainer()
                        .UseCompose().ServiceName(container.ID.ToString())
                        .FromFile(filePath)
                        .RemoveOrphans()
                        .Build().Start();

            container.Active = true;
            container.DockerID = svc.Containers.FirstOrDefault(x => x.Id != "").Id;

            await db.DockerContainer.UpdateAsync(container);
        }
    }
}