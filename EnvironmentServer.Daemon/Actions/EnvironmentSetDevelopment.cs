using CliWrap;
using EnvironmentServer.DAL;
using EnvironmentServer.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions;

public class EnvironmentSetDevelopment : ActionBase
{
    private const string PatternSW6AppEnv = "(APP_ENV=\")(.*)(\")";

    public override string ActionIdentifier => "environment_set_dev";

    public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
    {
        var db = sp.GetService<Database>();
        var em = sp.GetService<IExternalMessaging>();
        var env = db.Environments.Get(variableID);
        var usr = db.Users.GetByID(env.UserID);
        var path = $"/home/{usr.Username}/files/{env.InternalName}/.env";

        if (!File.Exists(path))
        {
            if (!string.IsNullOrEmpty(usr.UserInformation.SlackID))
            {
                await em.SendMessageAsync(string.Format(db.Settings.Get("environment_set_dev_error_404").Value, env.InternalName),
                    usr.UserInformation.SlackID);
            }
            db.Environments.SetTaskRunning(variableID, false);
            return;
        }

        var conf = File.ReadAllText(path);

        conf = Regex.Replace(conf, PatternSW6AppEnv, env.DevelopmentMode ? "$1prod$3" : "$1dev$3");
        File.WriteAllText(path, conf);

        await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"chown {usr.Username}:sftp_users /home/{usr.Username}/files/{env.InternalName}\"")
                .ExecuteAsync();

        db.Environments.SetDevelopmentMode(variableID, !env.DevelopmentMode);

        if (!string.IsNullOrEmpty(usr.UserInformation.SlackID))
        {
            await em.SendMessageAsync(string.Format(db.Settings.Get("environment_set_dev_finished").Value, env.InternalName, !env.DevelopmentMode ? "Enabled" : "Disabled"),
                usr.UserInformation.SlackID);
        }
        db.Environments.SetTaskRunning(variableID, false);
    }
}