using CliWrap;
using EnvironmentServer.DAL;
using EnvironmentServer.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

            var url = System.IO.File.ReadAllText($"/home/{user.Username}/files/{env.InternalName}/dl.txt");
            var filename = url.Substring(url.LastIndexOf('/') + 1);

            db.Logs.Add("Daemon", "download_extract for: " + env.InternalName + ", " + user.Username + " LINK: " + url);

            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"rm dl.txt\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.InternalName}")
                .ExecuteAsync();

            if (!Directory.Exists("/root/env/dl-cache"))
                Directory.CreateDirectory("/root/env/dl-cache/");

            if (File.Exists("/root/env/dl-cache/" + filename))
            {
                db.Logs.Add("Daemon", "File found for: " + env.InternalName + " File: " + url);
                db.Logs.Add("Daemon", "Unzip File for: " + env.InternalName);
                await Cli.Wrap("/bin/bash")
                    .WithArguments($"-c \"unzip /root/env/dl-cache/{filename}\"")
                    .WithWorkingDirectory($"/home/{user.Username}/files/{env.InternalName}")
                    .ExecuteAsync();
            }
            else if (filename.Contains("install_"))
            {
                db.Logs.Add("Daemon", "Download File for: " + env.InternalName + " File: " + url);
                await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"wget {url} -O /root/env/dl-cache/{filename}\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.InternalName}")
                .ExecuteAsync();

                db.Logs.Add("Daemon", "Unzip File for: " + env.InternalName);
                await Cli.Wrap("/bin/bash")
                    .WithArguments($"-c \"unzip /root/env/dl-cache/{filename}\"")
                    .WithWorkingDirectory($"/home/{user.Username}/files/{env.InternalName}")
                    .ExecuteAsync();
            }
            else
            {
                db.Logs.Add("Daemon", "Download File for: " + env.InternalName + " File: " + url);
                await Cli.Wrap("/bin/bash")
                    .WithArguments($"-c \"wget {url} -O /home/{user.Username}/files/{env.InternalName}/{filename}\"")
                    .WithWorkingDirectory($"/home/{user.Username}/files/{env.InternalName}")
                    .ExecuteAsync();

                db.Logs.Add("Daemon", "Unzip File for: " + env.InternalName);
                await Cli.Wrap("/bin/bash")
                    .WithArguments($"-c \"unzip {filename}\"")
                    .WithWorkingDirectory($"/home/{user.Username}/files/{env.InternalName}")
                    .ExecuteAsync();
            }

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"chown -R {user.Username} /home/{user.Username}/files/{env.InternalName}\"")
                .ExecuteAsync();

            db.Environments.SetTaskRunning(env.ID, false);

            var usr = db.Users.GetByID(env.UserID);
            if (!string.IsNullOrEmpty(usr.UserInformation.SlackID))
            {
                var success = await em.SendMessageAsync(string.Format(db.Settings.Get("slack_download_finished").Value, env.InternalName),
                    usr.UserInformation.SlackID);
                if (success)
                    return;
            }
            db.Mail.Send($"Download and Extract finished for {env.InternalName}!",
                string.Format(db.Settings.Get("mail_download_finished").Value, user.Username, env.InternalName), user.Email);
        }
    }
}
