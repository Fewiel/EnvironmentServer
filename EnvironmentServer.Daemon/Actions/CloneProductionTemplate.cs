using EnvironmentServer.DAL;
using EnvironmentServer.Interfaces;
using EnvironmentServer.Util;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions;

public class CloneProductionTemplate : ActionBase
{
    public override string ActionIdentifier => "clone_production_template";

    public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
    {
        var db = sp.GetService<Database>();
        var em = sp.GetService<IExternalMessaging>();
        var user = db.Users.GetByID(userID);
        var env = db.Environments.Get(variableID);
        var homeDir = $"/home/{user.Username}/files/{env.InternalName}";

        var version = File.ReadAllText($"/home/{user.Username}/files/{env.InternalName}/version.txt");
        File.Delete($"/home/{user.Username}/files/{env.InternalName}/version.txt");

        await Bash.CommandAsync($"git clone --branch v{version} https://github.com/shopware/production.git {homeDir}", homeDir);

        await Bash.CommandAsync($"composer install -q", homeDir, validation: false);

        File.Delete($"{homeDir}/vendor/shopware/recovery/composer.lock");
        File.Delete($"{homeDir}/vendor/shopware/recovery/Common/composer.lock");

        File.Move($"{homeDir}/public/.htaccess.dist", $"{homeDir}/public/.htaccess");

        await Bash.CommandAsync($"bin/console assets:install", homeDir);

        await Bash.ChownAsync(user.Username, "sftp_users", homeDir, true);

        db.Environments.SetTaskRunning(env.ID, false);

        if (!string.IsNullOrEmpty(user.UserInformation.SlackID))
        {
            var success = await em.SendMessageAsync($"Download of {env.InternalName} is finished",
                user.UserInformation.SlackID);
            if (success)
                return;
        }
        db.Mail.Send($"Installation finished for {env.InternalName}!", $"Download of {env.InternalName} is finished", user.Email);
    }
}