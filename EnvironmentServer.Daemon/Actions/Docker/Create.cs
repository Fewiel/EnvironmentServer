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
using Ductus.FluentDocker.Model.Containers;

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
                db.Logs.Add("docker.create", "Docker not found on Host");
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
                db.DockerPort.Insert(new() { Port = int.Parse(dp.Value), Name = dp.Key, DockerContainerID = container.ID });
            }

            var filePath = $"/root/DockerFiles/{container.ID}.yml";

            File.WriteAllText(filePath, dockerFile.Content);

            var svc = new Builder()
                        .UseContainer().WithName($"docker_container_{container.ID}")
                        .UseCompose()
                        .FromFile(filePath)
                        .RemoveOrphans()
                        .Build().Start();

            container.Active = true;
            container.DockerID = $"docker_container_{container.ID}";

            await db.DockerContainer.UpdateAsync(container);
        }
    }
}