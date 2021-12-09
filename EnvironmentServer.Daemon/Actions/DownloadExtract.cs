using CliWrap;
using EnvironmentServer.DAL;
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

        public override async Task ExecuteAsync(Database db, long variableID, long userID)
        {
            var user = db.Users.GetByID(userID);
            var env = db.Environments.Get(variableID);

            var url = System.IO.File.ReadAllText($"/home/{user.Username}/files/{env.Name}/dl.txt");
            var filename = url.Substring(url.LastIndexOf('/') + 1);

            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"rm dl.txt\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
                .ExecuteAsync();
            if (!Directory.Exists("/root/env/dl-cache"))
                Directory.CreateDirectory("/root/env/dl-cache/");

            if (File.Exists("/root/env/dl-cache/" + filename))
            {
                db.Logs.Add("Daemon", "File found for: " + env.Name + " File: " + url);
                db.Logs.Add("Daemon", "Unzip File for: " + env.Name);
                await Cli.Wrap("/bin/bash")
                    .WithArguments($"-c \"unzip /root/env/dl-cache/{filename}\"")
                    .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
                    .ExecuteAsync();
            }
            else
            {
                db.Logs.Add("Daemon", "Download File for: " + env.Name + " File: " + url);
                await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"wget {url} -O /root/env/dl-cache/{filename}\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
                .ExecuteAsync();

                db.Logs.Add("Daemon", "Unzip File for: " + env.Name);
                await Cli.Wrap("/bin/bash")
                    .WithArguments($"-c \"unzip /root/env/dl-cache/{filename}\"")
                    .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
                    .ExecuteAsync();
            }
            
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"chown -R {user.Username} /home/{user.Username}/files/{env.Name}\"")
                .ExecuteAsync();

            db.Environments.SetTaskRunning(env.ID, false);

            db.Mail.Send($"Download and Extract finished for {env.Name}!", string.Format(db.Settings.Get("mail_download_finished").Value, user.Username, env.Name), user.Email);
        }
    }
}
