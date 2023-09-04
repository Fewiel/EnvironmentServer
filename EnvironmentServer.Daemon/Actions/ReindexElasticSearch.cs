using EnvironmentServer.DAL;
using EnvironmentServer.Interfaces;
using EnvironmentServer.Util;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions;

public class ReindexElasticSearch : ActionBase
{
    public override string ActionIdentifier => "reindex_es";

    public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
    {
        var db = sp.GetService<Database>();
        var em = sp.GetService<IExternalMessaging>();
        var user = db.Users.GetByID(userID);
        var env = db.Environments.Get(variableID);
        var homeDir = $"/home/{user.Username}/files/{env.InternalName}";

        var log = $"Reindex Log for {env.DisplayName} - UTC: {DateTime.UtcNow} {Environment.NewLine}";
        log += $"Start UTC: {DateTime.UtcNow} {Environment.NewLine}";

        log += await Bash.CommandQueryAsync("bin/console es:reset", homeDir);
        log += Environment.NewLine;
        log += await Bash.CommandQueryAsync("bin/console cache:clear", homeDir);
        log += Environment.NewLine;
        log += await Bash.CommandQueryAsync("bin/console es:index", homeDir);
        log += Environment.NewLine;
        log += await Bash.CommandQueryAsync("bin/console dal:refresh:index", homeDir);
        log += Environment.NewLine;
        log += await Bash.CommandQueryAsync("bin/console es:create:alias", homeDir);
        log += Environment.NewLine;

        log += $"End UTC: {DateTime.UtcNow} {Environment.NewLine}";

        log = Regex.Replace(log, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);

        if (!string.IsNullOrEmpty(user.UserInformation.SlackID))
        {
            var success = await em.SendMessageAsync($"Reindex of Environment {env.DisplayName} finished. {Environment.NewLine} {log}",
                user.UserInformation.SlackID);
            if (success)
                return;
        }
        db.Mail.Send($"{env.DisplayName} - Reindex of ElasticSearch finished", $"Reindex of Environment {env.DisplayName} finished. {Environment.NewLine} {log}", user.Email);
    }
}
