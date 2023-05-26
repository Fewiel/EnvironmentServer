﻿using EnvironmentServer.Daemon.Models;
using EnvironmentServer.DAL;
using EnvironmentServer.Interfaces;
using EnvironmentServer.Util;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions
{
    internal class CloneProductionTemplateAutoinstall : ActionBase
    {
        private const string DatabaseUrl = "(DATABASE_URL=)(.*)";
        private const string AppUrl = "(APP_URL=)(.*)";

        public override string ActionIdentifier => "clone_production_template_install";

        public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
        {
            var db = sp.GetService<Database>();
            var em = sp.GetService<IExternalMessaging>();
            var user = db.Users.GetByID(userID);
            var env = db.Environments.Get(variableID);
            var homeDir = $"/home/{user.Username}/files/{env.InternalName}";

            var version = File.ReadAllText($"/home/{user.Username}/files/{env.InternalName}/version.txt");
            File.Delete($"/home/{user.Username}/files/{env.InternalName}/version.txt");

            var swVersion = new ShopwareVersion(version);

            if (swVersion.Major == 4 && swVersion.Minor >= 18 || swVersion.Major == 5)
            {
                Directory.Delete(homeDir, true);
                await Bash.CommandAsync($"shopware-cli project create {homeDir} {version}", homeDir);
            }
            else
            {
                await Bash.CommandAsync($"git clone --branch v{version} https://github.com/shopware/production.git {homeDir}", homeDir);
                if (File.Exists($"{homeDir}/vendor/shopware/recovery/composer.lock"))
                    File.Delete($"{homeDir}/vendor/shopware/recovery/composer.lock");
                if (File.Exists($"{homeDir}/vendor/shopware/recovery/Common/composer.lock"))
                    File.Delete($"{homeDir}/vendor/shopware/recovery/Common/composer.lock");

                await Bash.CommandAsync($"composer install -q", homeDir, validation: false);

                if (File.Exists($"{homeDir}/public/.htaccess.dist") && !File.Exists($"{homeDir}/public/.htaccess"))
                    File.Move($"{homeDir}/public/.htaccess.dist", $"{homeDir}/public/.htaccess");
            }

            if (swVersion.Major >= 5)
            {
                await Bash.CommandAsync($"bin/console assets:install", homeDir, validation: false);

                await Bash.CommandAsync($"php bin/console system:setup --app-env=\\\"prod\\\" " +
                        $"--env=\\\"prod\\\" -f -vvv " +
                        $"--database-url=\\\"mysql://{user.Username}_{env.InternalName}:{env.DBPassword}@localhost:3306/{user.Username}_{env.InternalName}\\\" " +
                        $"--app-url=\\\"https://{env.Address}\\\" " +
                        $"--composer-home=\\\"/home/{user.Username}/files/{env.InternalName}/var/cache/composer\\\" " +
                        $"--app-env=\\\"prod\\\" -n",
                        $"/home/{user.Username}/files/{env.InternalName}");

                await Bash.CommandAsync($"php bin/console system:install --create-database --basic-setup " +
                        $"--shop-name=\\\"{env.DisplayName}\\\" --shop-email=\\\"{user.Email}\\\" " +
                        $"--shop-locale=\\\"de_DE\\\" --shop-currency=\\\"EUR\\\" -n",
                        $"/home/{user.Username}/files/{env.InternalName}", validation: false);
            }
            else
            {
                var path = $"/home/{user.Username}/files/{env.InternalName}/.env";
                var conf = File.ReadAllText(path);
                conf = Regex.Replace(conf, AppUrl, $"$1https://{env.Address}");
                conf = Regex.Replace(conf, DatabaseUrl, $"$1mysql://{user.Username}_{env.InternalName}:{env.DBPassword}@localhost:3306/{user.Username}_{env.InternalName}");
                File.WriteAllText(path, conf);

                await Bash.CommandAsync($"bin/console system:install --basic-setup", homeDir, validation: true);
            }

            await Bash.CommandAsync($"php bin/console user:change-password admin -p {env.DBPassword}", homeDir);

            await Bash.ChownAsync(user.Username, "sftp_users", homeDir, true);

            db.Environments.SetTaskRunning(env.ID, false);

            if (!string.IsNullOrEmpty(user.UserInformation.SlackID))
            {
                var success = await em.SendMessageAsync($"Installation of {env.InternalName} is finished - Your \"admin\" password is {env.DBPassword}",
                    user.UserInformation.SlackID);
                if (success)
                    return;
            }
            db.Mail.Send($"Installation finished for {env.InternalName}!", $"Installation of {env.InternalName} is finished - Your \"admin\" password is {env.DBPassword}", user.Email);
        }
    }
}
