using CliWrap;
using EnvironmentServer.Daemon.Utility;
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

        if (!env.Stored)
            return;

        db.Logs.Add("Daemon", $"Starting restore for Environment {env.InternalName}.");

        await EnvironmentPacker.UnpackEnvironmentAsync(sp, env);

        db.Logs.Add("Daemon", $"Environment {env.InternalName} restored.");
        await em.SendMessageAsync($"Restore of Environment {env.InternalName} done.",
            db.UserInformation.Get(env.UserID).SlackID);
    }
}