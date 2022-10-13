using EnvironmentServer.DAL;
using EnvironmentServer.Interfaces;
using EnvironmentServer.Util;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions;

internal class DownloadExtractAutoinstall : ActionBase
{
    public override string ActionIdentifier => "download_extract_autoinstall";

    public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
    {
        var db = sp.GetService<Database>();
        var em = sp.GetService<IExternalMessaging>();
        var user = db.Users.GetByID(userID);
        var env = db.Environments.Get(variableID);

        var url = File.ReadAllText($"/home/{user.Username}/files/{env.InternalName}/dl.txt");
        var filename = url[(url.LastIndexOf('/') + 1)..];

        db.Logs.Add("Daemon", "download_extract_autoinstall for: " + env.InternalName + ", " + user.Username + " LINK: " + url);

        await Bash.CommandAsync($"rm dl.txt", $"/home/{user.Username}/files/{env.InternalName}");

        if (!Directory.Exists("/root/env/dl-cache"))
            Directory.CreateDirectory("/root/env/dl-cache/");

        if (File.Exists("/root/env/dl-cache/" + filename))
        {
            db.Logs.Add("Daemon", "File found for: " + env.InternalName + " File: " + url);
            db.Logs.Add("Daemon", "Unzip File for: " + env.InternalName);

            await Bash.CommandAsync($"unzip /root/env/dl-cache/{filename}", $"/home/{user.Username}/files/{env.InternalName}", validation: false);
        }
        else
        {
            db.Logs.Add("Daemon", "Download File for: " + env.InternalName + " File: " + url);

            await Bash.CommandAsync($"wget {url} -O /root/env/dl-cache/{filename}");

            db.Logs.Add("Daemon", "Unzip File for: " + env.InternalName);

            await Bash.CommandAsync($"unzip /root/env/dl-cache/{filename}", $"/home/{user.Username}/files/{env.InternalName}", validation: false);
        }

        await Bash.ChownAsync(user.Username, "sftp_users", $"/home/{user.Username}/files/{env.InternalName}", true);

        var dbname = user.Username + "_" + env.InternalName;
        var envVersion = env.Settings.Find(s => s.EnvironmentSetting.Property == "sw_version");

        db.Logs.Add("Daemon", "Unzip File for: " + env.InternalName);

        try
        {
            if (envVersion.Value[0] == '6')
            {
                //SW6
                await Bash.CommandAsync($"php8.0 bin/console system:setup  --app-env=\\\"prod\\\" " +
                    $"--env=\\\"prod\\\" -f -vvv " +
                    $"--database-url=\\\"mysql://{user.Username}_{env.InternalName}:{env.DBPassword}@localhost:3306/{user.Username}_{env.InternalName}\\\" " +
                    $"--app-url=\\\"https://{env.Address}\\\" " +
                    $"--composer-home=\\\"/home/{user.Username}/files/{env.InternalName}/var/cache/composer\\\" " +
                    $"--app-env=\\\"prod\\\" -n",
                    $"/home/{user.Username}/files/{env.InternalName}");

                await Bash.CommandAsync($"php8.0 bin/console system:install --create-database --basic-setup " +
                    $"--shop-locale=\\\"de-DE\\\" --shop-name=\\\"{env.DisplayName}\\\" --shop-email=\\\"{user.Email}\\\" " +
                    $"--shop-currency=\\\"EUR\\\" -n",
                    $"/home/{user.Username}/files/{env.InternalName}", validation: false);
            }
            else
            {
                //SW5
                await Bash.CommandAsync($"php7.4 recovery/install/index.php --no-interaction --quiet " +
                    $"--no-skip-import --db-host=\\\"localhost\\\" --db-user=\\\"{dbname}\\\" " +
                    $"--db-password=\\\"{env.DBPassword}\\\" --db-name=\\\"{dbname}\\\" " +
                    $"--shop-locale=\\\"de_DE\\\" --shop-host=\\\"{env.Address}\\\" " +
                    $"--shop-name=\\\"{env.InternalName}\\\" --shop-email=\\\"{user.Email}\\\" " +
                    $"--shop-currency=\\\"EUR\\\" --admin-username=\\\"admin\\\" --admin-password=\\\"shopware\\\" " +
                    $"--admin-email=\\\"{user.Email}\\\" --admin-name=\\\"Shopware Demo\\\" --admin-locale=\\\"de_DE\\\"",
                    $"/home/{user.Username}/files/{env.InternalName}");                
            }

            await Bash.ChownAsync(user.Username, "sftp_users", $"/home/{user.Username}/files/{env.InternalName}", true);

            db.Environments.SetTaskRunning(env.ID, false);

            var adminlink = env.Address + (envVersion.Value[0] == '6' ? "/admin" : "/backend");

            if (envVersion.Value[0] != '6')
                await db.Environments.SetSSLAsync(dbname);

            if (!string.IsNullOrEmpty(user.UserInformation.SlackID))
            {
                var success = await em.SendMessageAsync(string.Format(db.Settings.Get("slack_autoinstall_finished").Value, 
                    env.InternalName, env.Address, adminlink),
                    user.UserInformation.SlackID);
                if (success)
                    return;
            }
            db.Mail.Send($"Installation finished for {env.InternalName}!", string.Format(
                db.Settings.Get("mail_download_autoinstall_finished").Value, user.Username, env.InternalName, env.Address, adminlink), user.Email);

        }
        catch (Exception ex)
        {
            db.Environments.SetTaskRunning(env.ID, false);
            if (!string.IsNullOrEmpty(user.UserInformation.SlackID))
            {
                var success = await em.SendMessageAsync(string.Format(db.Settings.Get("slack_install_failed").Value, env.InternalName, ex),
                    user.UserInformation.SlackID);
                if (success)
                    return;
            }
            db.Mail.Send($"Installation finished for {env.InternalName}!", string.Format(
                db.Settings.Get("mail_install_failed").Value, user.Username, env.InternalName, ex), user.Email);
        }
    }
}