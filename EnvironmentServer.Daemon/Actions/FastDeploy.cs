using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Builders;
using EnvironmentServer.Daemon.Utility;
using EnvironmentServer.DAL;
using EnvironmentServer.DAL.StringConstructors;
using EnvironmentServer.Interfaces;
using EnvironmentServer.Util;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;
using EnvironmentServer.DAL.Repositories;
using System.Text.RegularExpressions;

namespace EnvironmentServer.Daemon.Actions;

public class FastDeploy : ActionBase
{
    public override string ActionIdentifier => "fast_deploy";

    private const string PatternSW6ESHost = "(SHOPWARE_ES_HOSTS=\")(.*)(\")";
    private const string PatternSW6ESEnabled = "(SHOPWARE_ES_ENABLED=\")(.*)(\")";
    private const string PatternSW6ESIndexing = "(SHOPWARE_ES_INDEXING_ENABLED=\")(.*)(\")";

    public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
    {
        var db = sp.GetService<Database>();
        var em = sp.GetService<IExternalMessaging>();
        var env = db.Environments.Get(variableID);
        var usr = db.Users.GetByID(env.UserID);
        var tmpID = System.IO.File.ReadAllText($"/home/{usr.Username}/files/{env.InternalName}/template.txt");

        await EnvironmentPacker.DeployTemplateAsync(db, env, long.Parse(tmpID));

        if (!string.IsNullOrEmpty(usr.UserInformation.SlackID))
        {
            await em.SendMessageAsync(string.Format(db.Settings.Get("fast_deploy_finished").Value, env.InternalName),
                usr.UserInformation.SlackID);
        }

        if (System.IO.File.Exists($"/home/{usr.Username}/files/{env.InternalName}/template.sh"))
        {
            await Bash.CommandAsync($"bash template.sh", $"/home/{usr.Username}/files/{env.InternalName}");
            await Bash.ChownAsync(usr.Username, "sftp_users", $"/home/{usr.Username}/files/{env.InternalName}");
        }

        if (System.IO.File.Exists($"/home/{usr.Username}/files/{env.InternalName}/.elasticsearch"))
        {
            var cfid = System.IO.File.ReadAllText($"/home/{usr.Username}/files/{env.InternalName}/.elasticsearch");
            var port = await SetupESAsync(sp, usr.ID, long.Parse(cfid), env.InternalName);

            var cnf = System.IO.File.ReadAllText($"/home/{usr.Username}/files/{env.InternalName}/.env");
            cnf = Regex.Replace(cnf, PatternSW6ESHost, $"SHOPWARE_ES_HOSTS=\"localhost:{port}\"");
            cnf = Regex.Replace(cnf, PatternSW6ESEnabled, "SHOPWARE_ES_ENABLED=\"1\"");
            cnf = Regex.Replace(cnf, PatternSW6ESIndexing, "SHOPWARE_ES_INDEXING_ENABLED=\"1\"");
            System.IO.File.WriteAllText($"/home/{usr.Username}/files/{env.InternalName}/.env", cnf);
        }

        db.Environments.SetTaskRunning(env.ID, false);
    }

    private async Task<int> SetupESAsync(ServiceProvider sp, long usrID, long cfID, string envName)
    {
        var hosts = new Hosts().Discover();
        var _docker = hosts.FirstOrDefault(x => x.IsNative) ?? hosts.FirstOrDefault(x => x.Name == "default");
        var db = sp.GetService<Database>();

        if (_docker == null)
        {
            db.Logs.Add("docker.start", "Docker not found on Host");
            return 0;
        }

        var containerId = await db.DockerContainer.InsertAsync(new DAL.Models.DockerContainer
        {
            UserID = usrID,
            DockerComposeFileID = cfID,
            Name = $"ElasticSearch Autogen {envName}"
        });

        var container = await db.DockerContainer.GetByIDAsync(containerId);
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

        if (dockerFile.Variables.TryGetValue("http", out var port))
        {
            var config = ProxyConfConstructor.Construct.WithPort(port)
                    .WithDomain($"web-container-{container.ID}.{db.Settings.Get("domain").Value}").BuildHttpProxy();

            System.IO.File.WriteAllText($"/etc/apache2/sites-available/web-container-{container.ID}.conf", config);

            await Bash.CommandAsync($"a2ensite web-container-{container.ID}.conf");
            await Bash.ReloadApacheAsync();
        }

        if (dockerFile.Variables.TryGetValue("https", out var portssl))
        {
            var config = ProxyConfConstructor.Construct.WithPort(portssl)
                    .WithDomain($"ssl-container-{container.ID}.{db.Settings.Get("domain").Value}").BuildHttpsProxy();

            System.IO.File.WriteAllText($"/etc/apache2/sites-available/ssl-container-{container.ID}.conf", config);

            await Bash.CommandAsync($"a2ensite ssl-container-{container.ID}.conf");
            await Bash.ReloadApacheAsync();
        }

        var filePath = $"/root/DockerFiles/{container.ID}.yml";

        System.IO.File.WriteAllText(filePath, dockerFile.Content);

        var svc = new Builder()
                    .UseContainer()
                    .UseCompose().ServiceName(container.ID.ToString())
                    .FromFile(filePath)
                    .RemoveOrphans()
                    .Build().Start();

        container.Active = true;
        container.DockerID = svc.Containers.FirstOrDefault(x => x.Id != "").Id;

        await db.DockerContainer.UpdateAsync(container);

        return port;
    }
}
