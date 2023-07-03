﻿using EnvironmentServer.DAL;
using EnvironmentServer.Interfaces;
using EnvironmentServer.Util;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions;

public class ExtensionTestingInstall : ActionBase
{
    public override string ActionIdentifier => "extension_testing_install";

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

        if (File.Exists($"{homeDir}/vendor/shopware/recovery/composer.lock"))
            File.Delete($"{homeDir}/vendor/shopware/recovery/composer.lock");
        if (File.Exists($"{homeDir}/vendor/shopware/recovery/Common/composer.lock"))
            File.Delete($"{homeDir}/vendor/shopware/recovery/Common/composer.lock");

        await Bash.CommandAsync($"composer install -q", homeDir, validation: false);

        if (File.Exists($"{homeDir}/public/.htaccess.dist"))
            File.Move($"{homeDir}/public/.htaccess.dist", $"{homeDir}/public/.htaccess");

        await Bash.CommandAsync($"php bin/console assets:install", homeDir, validation: false);

        await Bash.CommandAsync($"php bin/console system:setup --app-env=\\\"prod\\\" " +
                $"--env=\\\"prod\\\" -f -vvv " +
                $"--database-url=\\\"mysql://{user.Username}_{env.InternalName}:{env.DBPassword}@localhost:3306/{user.Username}_{env.InternalName}\\\" " +
                $"--app-url=\\\"https://{env.Address}\\\" " +
                $"--composer-home=\\\"/home/{user.Username}/files/{env.InternalName}/var/cache/composer\\\" " +
                $"--app-env=\\\"prod\\\" -n",
                $"/home/{user.Username}/files/{env.InternalName}");

        if (version.ToLower().StartsWith("6.5"))
        {
            await Bash.CommandAsync($"composer setup -q", homeDir, validation: false);
        }
        else
        {
            await Bash.CommandAsync($"php bin/console system:install --create-database --basic-setup " +
            $"--shop-name=\\\"{env.DisplayName}\\\" --shop-email=\\\"{user.Email}\\\" " +
            $"--shop-locale=\\\"de_DE\\\" --shop-currency=\\\"EUR\\\" -n",
            $"/home/{user.Username}/files/{env.InternalName}", validation: false);
        }
        await Bash.CommandAsync($"php bin/console user:change-password admin -p {env.DBPassword}", homeDir);
        await Bash.ChownAsync(user.Username, "sftp_users", homeDir, true);

        db.Environments.SetTaskRunning(env.ID, false);
    }
}