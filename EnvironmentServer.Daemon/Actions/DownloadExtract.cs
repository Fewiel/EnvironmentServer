using EnvironmentServer.DAL;
using EnvironmentServer.Interfaces;
using EnvironmentServer.Util;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions
{
    class DownloadExtract : ActionBase
    {
        public override string ActionIdentifier => "download_extract";

        public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
        {
            var db = sp.GetService<Database>();
            var em = sp.GetService<IExternalMessaging>();
            var user = db.Users.GetByID(userID);
            var env = db.Environments.Get(variableID);

            var url = File.ReadAllText($"/home/{user.Username}/files/{env.InternalName}/dl.txt");
            var filename = url.Substring(url.LastIndexOf('/') + 1);

            db.Logs.Add("Daemon", "download_extract for: " + env.InternalName + ", " + user.Username + " LINK: " + url);

            await Bash.CommandAsync("rm dl.txt", $"/home/{user.Username}/files/{env.InternalName}");

            if (!Directory.Exists("/root/env/dl-cache"))
                Directory.CreateDirectory("/root/env/dl-cache/");

            if (File.Exists("/root/env/dl-cache/" + filename))
            {
                db.Logs.Add("Daemon", "File found for: " + env.InternalName + " File: " + url);
                db.Logs.Add("Daemon", "Unzip File for: " + env.InternalName);

                await Bash.CommandAsync($"unzip /root/env/dl-cache/{filename}", $"/home/{user.Username}/files/{env.InternalName}", validation: false);
            }
            else if (filename.Contains("install_"))
            {
                db.Logs.Add("Daemon", "Download File for: " + env.InternalName + " File: " + url);

                await Bash.CommandAsync($"wget {url} -O /root/env/dl-cache/{filename}", validation: false);

                db.Logs.Add("Daemon", "Unzip File for: " + env.InternalName);

                await Bash.CommandAsync($"unzip /root/env/dl-cache/{filename}", $"/home/{user.Username}/files/{env.InternalName}", validation: false);
            }
            else
            {
                db.Logs.Add("Daemon", "Download File for: " + env.InternalName + " File: " + url);

                await Bash.CommandAsync($"wget {url} -O /home/{user.Username}/files/{env.InternalName}/{filename}", validation: false);

                db.Logs.Add("Daemon", "Unzip File for: " + env.InternalName);

                await Bash.CommandAsync($"unzip {filename}", $"/home/{user.Username}/files/{env.InternalName}", validation: false);
            }

            await Bash.ChownAsync(user.Username, "sftp_users", $"/home/{user.Username}/files/{env.InternalName}", true);

            db.Environments.SetTaskRunning(env.ID, false);

            var usr = db.Users.GetByID(env.UserID);
            if (!string.IsNullOrEmpty(usr.UserInformation.SlackID))
            {
                var success = await em.SendMessageAsync(string.Format(db.Settings.Get("slack_download_finished").Value, env.InternalName, env.Address),
                    usr.UserInformation.SlackID);
                if (success)
                    return;
            }
            db.Mail.Send($"Download and Extract finished for {env.InternalName}!",
                string.Format(db.Settings.Get("mail_download_finished").Value, user.Username, env.InternalName, env.Address), user.Email);
        }
    }
}
