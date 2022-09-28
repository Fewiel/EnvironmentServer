using EnvironmentServer.DAL;
using EnvironmentServer.Interfaces;
using EnvironmentServer.Util;
using Microsoft.Extensions.DependencyInjection;
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

            await Bash.CommandAsync("rm dl.txt", $"/home/{user.Username}/files/{env.InternalName}");

            db.Logs.Add("Daemon", "Clone Repo for: " + env.InternalName + " URL: " + url);

            await Bash.CommandAsync("git init", $"/home/{user.Username}/files/{env.InternalName}");

            var repo = url.Substring(0, url.IndexOf("/commit/"));
            var hash = url.Substring(url.LastIndexOf('/') + 1);

            await Bash.CommandAsync($"git remote add origin {repo}", $"/home/{user.Username}/files/{env.InternalName}");
            await Bash.CommandAsync($"git fetch origin {hash}", $"/home/{user.Username}/files/{env.InternalName}");
            await Bash.CommandAsync($"git reset --hard FETCH_HEAD", $"/home/{user.Username}/files/{env.InternalName}");

            await Bash.ChownAsync(user.Username, "sftp_users", $"/home/{user.Username}/files/{env.InternalName}", true);

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
