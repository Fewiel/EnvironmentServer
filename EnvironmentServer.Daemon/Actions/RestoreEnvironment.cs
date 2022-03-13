using CliWrap;
using EnvironmentServer.DAL;
using EnvironmentServer.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions;

internal class RestoreEnvironment : ActionBase
{
    public override string ActionIdentifier => "restore_environment";

    public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
    {
        var db = sp.GetService<Database>();
        var em = sp.GetService<IExternalMessaging>();
        var env = db.Environments.Get(variableID);
        var usr = db.Users.GetByID(env.UserID);

        if (!File.Exists($"/home/{usr.Username}/files/inactive/{env.Name}.zip"))
        {
            db.Logs.Add("Daemon", $"Restore Failed! File not found: /home/{usr.Username}/files/inactive/{env.Name}.zip");
            await em.SendMessageAsync($"Restore of Environment Failed! File not found: /home/{usr.Username}/files/inactive/{env.Name}.zip",
                db.UserInformation.Get(usr.ID).SlackID);
            return;
        }

        Directory.Delete($"/home/{usr.Username}/files/{env.Name}", true);

        await Cli.Wrap("/bin/bash")
            .WithArguments($"-c \"unzip /home/{usr.Username}/files/inactive/{env.Name}.zip\"")
            .WithWorkingDirectory($"/home/{usr.Username}/files")
            .ExecuteAsync();

        await Cli.Wrap("/bin/bash")
            .WithArguments($"-c \"chown -R {usr.Username} /home/{usr.Username}/files/{env.Name}\"")
            .ExecuteAsync();

        File.Delete($"/home/{usr.Username}/files/inactive/{env.Name}.zip");

        db.Environments.SetStored(env.ID, false);

        db.Logs.Add("Daemon", $"Environment {env.Name} restored.");
        await em.SendMessageAsync($"Restore of Environment {env.Name} done.",
            db.UserInformation.Get(userID).SlackID);
    }
}