using CliWrap;
using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Interfaces;
using EnvironmentServer.SlackBot;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions
{
    internal class CloneGit : ActionBase
    {
        public override string ActionIdentifier => "clone_repo";

        public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
        {
            var db = sp.GetService<Database>();
            var em = sp.GetService<IExternalMessaging>();
            var user = db.Users.GetByID(userID);
            var env = db.Environments.Get(variableID);

            var url = System.IO.File.ReadAllText($"/home/{user.Username}/files/{env.InternalName}/dl.txt");

            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"rm dl.txt\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.InternalName}")
                .ExecuteAsync();

            db.Logs.Add("Daemon", "Clone Repo for: " + env.InternalName + " URL: " + url);

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"git init\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.InternalName}")
                .ExecuteAsync();

            var repo = url.Substring(0, url.IndexOf("/commit/"));
            var hash = url.Substring(url.LastIndexOf('/') + 1);

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"git remote add origin {repo}\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.InternalName}")
                .ExecuteAsync();

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"git fetch origin {hash}\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.InternalName}")
                .ExecuteAsync();

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"git reset --hard FETCH_HEAD\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.InternalName}")
                .ExecuteAsync();

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"chown -R {user.Username} /home/{user.Username}/files/{env.InternalName}\"")
                .ExecuteAsync();

            db.Environments.SetTaskRunning(env.ID, false);

            var usr = db.Users.GetByID(env.UserID);
            if (!string.IsNullOrEmpty(usr.UserInformation.SlackID))
            {
                var success = await em.SendMessageAsync(string.Format(db.Settings.Get("slack_clone_finished").Value, env.InternalName),
                    usr.UserInformation.SlackID);
                if (success)
                    return;
            }
            db.Mail.Send($"Setup ready for {env.InternalName}!", string.Format(db.Settings.Get("mail_setup_finished").Value,
                                user.Username, env.InternalName), user.Email);
        }
    }
}
