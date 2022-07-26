using Ductus.FluentDocker.Commands;
using Ductus.FluentDocker.Services;
using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using Ductus.FluentDocker.Model.Containers;
using System.Linq;
using System.Threading.Tasks;

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

            if (_docker == null)
            {
                db.Logs.Add("docker.start", "Docker not found");
                return Task.CompletedTask;
            }

            var id = _docker.Host.Run("docker.elastic.co/elasticsearch/elasticsearch:{esVersion}", new ContainerCreateParams
            {
                Name = "",
                PortMappings = new string[] { "127.0.0.1:{port}:9200", "127.0.0.1:{port + 100}:9300" },
                Environment = new string[] { "discovery.type=single-node" },
                Interactive = true
            }, _docker.Certificates).Data;
        }
    }
}