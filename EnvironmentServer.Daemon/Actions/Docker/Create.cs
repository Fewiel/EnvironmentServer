using Ductus.FluentDocker.Commands;
using Ductus.FluentDocker.Services;
using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using Ductus.FluentDocker.Builders;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions.Docker
{
    internal class Create : ActionBase
    {
        public override string ActionIdentifier => "docker.start";

        public override Task ExecuteAsync(ServiceProvider db, long variableID, long userID)
        {
            throw new System.NotImplementedException();
        }

        //public override Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
        //{
        //    var hosts = new Hosts().Discover();
        //    var _docker = hosts.FirstOrDefault(x => x.IsNative) ?? hosts.FirstOrDefault(x => x.Name == "default");
        //    var db = sp.GetService<Database>();

        //    if (_docker == null)
        //    {
        //        db.Logs.Add("docker.start", "Docker not found on Host");
        //        return Task.CompletedTask;
        //    }

        //    var svc = new Builder()
        //                .UseContainer()
        //                .UseCompose()
        //                .FromFile(file)
        //                .RemoveOrphans()
        //                .Build().Start())
        //}
    }
}