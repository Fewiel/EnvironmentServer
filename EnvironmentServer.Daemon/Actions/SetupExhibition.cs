using CliWrap;
using Dapper;
using EnvironmentServer.DAL;
using EnvironmentServer.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions;

public class SetupExhibition : ActionBase
{
    public override string ActionIdentifier => "setup_exhibition";

    public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
    {
        var db = sp.GetService<Database>();
        var em = sp.GetService<IExternalMessaging>();
        var user = db.Users.GetByID(userID);
        var env = db.Environments.Get(variableID);
        var dbString = user.Username + "_" + env.Name;
        var config = JsonConvert.DeserializeObject<DBConfig>(File.ReadAllText("DBConfig.json"));

        var filename = File.ReadAllText($"/home/{user.Username}/files/{env.Name}/dl.txt");

        db.Logs.Add("Daemon", "Setup Exhibition for: " + env.Name + ", " + user.Username);

        await Cli.Wrap("/bin/bash")
            .WithArguments("-c \"rm dl.txt\"")
            .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
            .ExecuteAsync();

        db.Logs.Add("Daemon", "Unzip File for: " + env.Name);
        await Cli.Wrap("/bin/bash")
            .WithArguments($"-c \"unzip -qq {filename}\"")
            .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        await Cli.Wrap("/bin/bash")
            .WithArguments($"-c \"mysql -u {config.Username} -p{config.Password} " + dbString + " < db.sql\"")
            .WithValidation(CommandResultValidation.None)
            .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
            .ExecuteAsync();

        await Cli.Wrap("/bin/bash")
            .WithArguments($"-c \"chown -R {user.Username} /home/{user.Username}/files/{env.Name}\"")
            .ExecuteAsync();

        var shopwareconfig = File.ReadAllText($"/home/{user.Username}/files/{env.Name}/.env");
        shopwareconfig = shopwareconfig + Environment.NewLine + $"DATABASE_URL=mysql://{user.Username}_{env.Name}:{env.DBPassword}@localhost:3306/{user.Username}_{env.Name}";
        File.WriteAllText($"/home/{user.Username}/files/{env.Name}/.env", shopwareconfig);

        using var conn = db.GetConnection();
        conn.Execute($"UPDATE `{user.Username}_{env.Name}`.`sales_channel_domain` SET `url` = @url WHERE `url` not like '%/de' and `url` like '%http%';", new
        {
            url = "https://" + env.Address
        });
        conn.Execute($"UPDATE `{user.Username}_{env.Name}`.`sales_channel_domain` SET `url` = @url WHERE `url` like '%/de' and `url` like '%http%';", new
        {
            url = "https://" + env.Address + "/de"
        });

        db.Environments.SetTaskRunning(env.ID, false);
        var usr = db.Users.GetByID(env.UserID);
        if (!string.IsNullOrEmpty(usr.UserInformation.SlackID))
        {
            var success = await em.SendMessageAsync(string.Format(db.Settings.Get("slack_download_finished").Value, env.Name),
                usr.UserInformation.SlackID);
            if (success)
                return;
        }
        db.Mail.Send($"Download and Extract finished for {env.Name}!",
            string.Format(db.Settings.Get("mail_download_finished").Value, user.Username, env.Name), user.Email);
    }
}
