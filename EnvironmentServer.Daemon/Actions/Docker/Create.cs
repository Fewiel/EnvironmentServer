using Ductus.FluentDocker.Commands;
using Ductus.FluentDocker.Services;
using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using Ductus.FluentDocker.Model.Containers;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using EnvironmentServer.DAL.Models;

namespace EnvironmentServer.Daemon.Actions.Docker
{
    internal class Create : ActionBase
    {
        public override string ActionIdentifier => "docker.start";

        public override Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
        {
            var hosts = new Hosts().Discover();
            var _docker = hosts.FirstOrDefault(x => x.IsNative) ?? hosts.FirstOrDefault(x => x.Name == "default");
            var db = sp.GetService<Database>();

            var config = JsonConvert.DeserializeObject<DockerInstance>(db.CmdActionDetail.Get(variableID).JsonString);

            if (_docker == null)
            {
                db.Logs.Add("docker.start", "Docker not found on Host");
                return Task.CompletedTask;
            }

            var id = _docker.Host.Run(config.Image, new ContainerCreateParams
            {
                Name = config.Name,
                PortMappings = config.PortMappings,
                Environment = config.DockerEnvironment,
                Interactive = config.Interactive
            }, _docker.Certificates).Data;

            config.ID = id;

        }
    }
}