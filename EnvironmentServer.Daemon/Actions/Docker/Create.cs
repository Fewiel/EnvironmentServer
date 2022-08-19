using Ductus.FluentDocker.Commands;
using Ductus.FluentDocker.Services;
using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using Ductus.FluentDocker.Builders;
using System.Linq;
using System.Threading.Tasks;
using EnvironmentServer.Daemon.Utility;
using System.IO;

namespace EnvironmentServer.Daemon.Actions.Docker
{
    internal class Create : ActionBase
    {
        public override string ActionIdentifier => "docker.start";

        public override Task ExecuteAsync(ServiceProvider db, long variableID, long userID)
        {
            throw new System.NotImplementedException();
        }

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

            var dockerFile = DockerFileBuilder.Build(fileTemplate.Content, usedPorts, minPort);

            foreach (var dp in dockerFile.Ports)
            {
                db.DockerPort.Insert(new() { Port = dp, DockerContainerID = container.ID });
            }

            var filePath = $"/root/DockerFiles/{container.ID}.yml";

            File.WriteAllText(filePath, dockerFile.Content);

            var svc = new Builder()
                        .UseContainer()
                        .UseCompose()
                        .FromFile(filePath)
                        .RemoveOrphans()
                        .Build().Start();

            container.Active = true;
            container.DockerID = svc.Name;
            container.LatestUse = System.DateTimeOffset.Now;

            await db.DockerContainer.UpdateAsync(container);
        }
    }
}