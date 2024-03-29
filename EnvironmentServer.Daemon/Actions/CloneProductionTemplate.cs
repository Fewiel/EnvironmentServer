﻿using EnvironmentServer.DAL;
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

        if (version.ToLower().Contains("rc"))
        {
            await Bash.CommandAsync($"git clone --branch v{version} https://github.com/shopware/platform.git {homeDir}", homeDir);
        }
        else if (version.ToLower().Contains("trunk"))
        {
            await Bash.CommandAsync($"git clone --branch trunk https://github.com/shopware/platform.git {homeDir}", homeDir);
        }
        else if (version.ToLower().StartsWith("6.5"))
        {
            Directory.CreateDirectory($"{homeDir}/public");
            await Bash.CommandAsync($"wget https://github.com/shopware/web-recovery/releases/latest/download/shopware-installer.phar.php shopware-installer.phar.php", $"{homeDir}/public", validation: false);
        }
        else
        {
            await Bash.CommandAsync($"git clone --branch v{version} https://github.com/shopware/production.git {homeDir}", homeDir);
        }

        if (!version.StartsWith("6.5"))
        {
            await Bash.CommandAsync($"composer install -q", homeDir, validation: false);

            if (File.Exists($"{homeDir}/vendor/shopware/recovery/composer.lock"))
                File.Delete($"{homeDir}/vendor/shopware/recovery/composer.lock");
            if (File.Exists($"{homeDir}/vendor/shopware/recovery/Common/composer.lock"))
                File.Delete($"{homeDir}/vendor/shopware/recovery/Common/composer.lock");

            if (File.Exists($"{homeDir}/public/.htaccess.dist"))
                File.Move($"{homeDir}/public/.htaccess.dist", $"{homeDir}/public/.htaccess");

            if (version.StartsWith("6.4"))
            {
                await Bash.CommandAsync($"php7.4 bin/console assets:install", homeDir, validation: false);
            }
            else
            {
                await Bash.CommandAsync($"php8.1 bin/console assets:install", homeDir, validation: false);
            }
        }

        await Bash.ChownAsync(user.Username, "sftp_users", homeDir, true);

        db.Environments.SetTaskRunning(env.ID, false);

        if (!string.IsNullOrEmpty(user.UserInformation.SlackID))
        {
            var success = await em.SendMessageAsync($"Download of {env.InternalName} is finished - Please use a secure password for your environment!",
                user.UserInformation.SlackID);
            if (success)
                return;
        }
        db.Mail.Send($"Download finished for {env.InternalName}!", $"Download of {env.InternalName} is finished - Please use a secure password for your environment", user.Email);
    }
}