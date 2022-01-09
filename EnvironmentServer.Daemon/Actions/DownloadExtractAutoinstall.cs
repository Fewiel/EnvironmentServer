using CliWrap;
using EnvironmentServer.DAL;
using EnvironmentServer.Interfaces;
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

        var url = System.IO.File.ReadAllText($"/home/{user.Username}/files/{env.Name}/dl.txt");
        var filename = url.Substring(url.LastIndexOf('/') + 1);

        db.Logs.Add("Daemon", "download_extract for: " + env.Name + ", " + user.Username + " LINK: " + url);

        await Cli.Wrap("/bin/bash")
            .WithArguments("-c \"rm dl.txt\"")
            .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
            .ExecuteAsync();
        if (!Directory.Exists("/root/env/dl-cache"))
            Directory.CreateDirectory("/root/env/dl-cache/");

        if (File.Exists("/root/env/dl-cache/" + filename))
        {
            db.Logs.Add("Daemon", "File found for: " + env.Name + " File: " + url);
            db.Logs.Add("Daemon", "Unzip File for: " + env.Name);
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"unzip /root/env/dl-cache/{filename}\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
                .ExecuteAsync();
        }
        else
        {
            db.Logs.Add("Daemon", "Download File for: " + env.Name + " File: " + url);
            await Cli.Wrap("/bin/bash")
            .WithArguments($"-c \"wget {url} -O /root/env/dl-cache/{filename}\"")
            .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
            .ExecuteAsync();

            db.Logs.Add("Daemon", "Unzip File for: " + env.Name);
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"unzip /root/env/dl-cache/{filename}\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
                .ExecuteAsync();
        }

        await Cli.Wrap("/bin/bash")
            .WithArguments($"-c \"chown -R {user.Username} /home/{user.Username}/files/{env.Name}\"")
            .ExecuteAsync();


        var dbname = user.Username + "_" + env.Name;
        var envVersion = env.Settings.Find(s => s.EnvironmentSetting.Property == "sw_version");

        db.Logs.Add("Daemon", "Unzip File for: " + env.Name);
                
        if (envVersion.Value[0] == '6')
        {
            //SW6
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"php7.4 public/recovery/install/index.php --db-host=\\\"localhost\\\" " +
                $"--db-port=\\\"3306\\\" --db-user=\\\"{dbname}\\\" --db-password=\\\"{env.DBPassword}\\\" " +
                $"--db-name=\\\"{dbname}\\\" --shop-locale=\\\"de-DE\\\" --no-skip-import " +
                $"--shop-host=\\\"{env.Address}\\\" --shop-email=\\\"{user.Email}\\\" --admin-username=\\\"demo\\\" " +
                $"--admin-password=\\\"demo\\\" --admin-email=\\\"{user.Email}\\\" --admin-firstname=\\\"Shopware\\\" " +
                $"--admin-lastname=\\\"Demo\\\" --shop-currency=\\\"EUR\\\" --shop-name=\\\"{env.Name}\\\" " +
                $"--shop-country=\\\"DEU\\\" --admin-locale=\\\"de-DE\\\" -n\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
                .ExecuteAsync();

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"bin/build.sh\"")
                .WithValidation(CommandResultValidation.None)
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"bin/build-js.sh\"")
                .WithValidation(CommandResultValidation.None)
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"php7.4 bin/console theme:change --all Storefront\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"php7.4 bin/console theme:compile\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"php7.4 bin/console cache:clear\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
                .ExecuteAsync();            
        }
        else
        {
            //SW5
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"php7.4 recovery/install/index.php --no-interaction --quiet " +
                $"--no-skip-import --db-host=\\\"localhost\\\" --db-user=\\\"{dbname}\\\" " +
                $"--db-password=\\\"{env.DBPassword}\\\" --db-name=\\\"{dbname}\\\" " +
                $"--shop-locale=\\\"de_DE\\\" --shop-host=\\\"{env.Address}\\\" " +
                $"--shop-name=\\\"{env.Name}\\\" --shop-email=\\\"{user.Email}\\\" " +
                $"--shop-currency=\\\"EUR\\\" --admin-username=\\\"demo\\\" --admin-password=\\\"demo\\\" " +
                $"--admin-email=\\\"{user.Email}\\\" --admin-name=\\\"Shopware Demo\\\" --admin-locale=\\\"de_DE\\\"\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
                .ExecuteAsync();
        }

        await Cli.Wrap("/bin/bash")
            .WithArguments($"-c \"chown -R {user.Username} /home/{user.Username}/files/{env.Name}\"")
            .ExecuteAsync();

        db.Environments.SetTaskRunning(env.ID, false);

        if (!string.IsNullOrEmpty(user.UserInformation.SlackID))
        {
            var success = await em.SendMessageAsync(string.Format(db.Settings.Get("slack_autoinstall_finished").Value, env.Name),
                user.UserInformation.SlackID);
            if (success)
                return;
        }
        db.Mail.Send($"Installation finished for {env.Name}!", string.Format(
            db.Settings.Get("mail_download_finished").Value, user.Username, env.Name), user.Email);
    }
}