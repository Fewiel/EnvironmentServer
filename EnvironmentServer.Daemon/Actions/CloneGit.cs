using CliWrap;
using EnvironmentServer.DAL;
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

        public override async Task ExecuteAsync(Database db, long variableID, long userID)
        {
            var user = db.Users.GetByID(userID);
            var env = db.Environments.Get(variableID);

            var url = System.IO.File.ReadAllText($"/home/{user.Username}/files/{env.Name}/dl.txt");

            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"rm dl.txt\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
                .ExecuteAsync();

            db.Logs.Add("Daemon", "Clone Repo for: " + env.Name + " URL: " + url);

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"git init\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
                .ExecuteAsync();

            var repo = url.Substring(0, url.IndexOf("/commit/"));
            var hash = url.Substring(url.LastIndexOf('/') + 1);

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"chown -R {user.Username} /home/{user.Username}/files/{env.Name}\"")
                .ExecuteAsync();

            db.Environments.SetTaskRunning(env.ID, false);

            db.Mail.Send($"Setup ready for {env.Name}!", string.Format(db.Settings.Get("mail_download_finished").Value, user.Username, env.Name), user.Email);
        }
    }
}
