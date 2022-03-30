using CliWrap;
using EnvironmentServer.DAL;
using EnvironmentServer.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions
{
    public class SnapshotCreate : ActionBase
    {
        public override string ActionIdentifier => "snapshot_create";

        public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
        {
            var db = sp.GetService<Database>();
            var em = sp.GetService<IExternalMessaging>();

            //Get user, environment and databasename            
            var user = db.Users.GetByID(userID);
            var snap = db.Snapshot.Get(variableID);
            var env = db.Environments.Get(snap.EnvironmentId);
            var dbString = user.Username + "_" + env.InternalName;
            var config = JsonConvert.DeserializeObject<DBConfig>(File.ReadAllText("DBConfig.json"));

            db.Logs.Add("Daemon", "SnapshotCreate - Disable Site: " + env.InternalName);
            //Stop Website (a2dissite)
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"a2dissite {user.Username}_{env.InternalName}.conf\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service apache2 reload\"")
                .ExecuteAsync();

            db.Logs.Add("Daemon", "SnapshotCreate - Create database dump: " + env.InternalName);
            //Create database dump in site folder
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"mysqldump -u {config.Username} -p{config.Password} " + dbString + " > db.sql\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.InternalName}")
                .ExecuteAsync();

            db.Logs.Add("Daemon", "SnapshotCreate - Git init: " + env.InternalName);
            //check for git init
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"git init\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.InternalName}")
                .ExecuteAsync();

            db.Logs.Add("Daemon", "SnapshotCreate - Git create: " + env.InternalName);
            //git stage
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"git stage -A\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.InternalName}")
                .ExecuteAsync();

            db.Logs.Add("Daemon", "SnapshotCreate - Git commit: " + env.InternalName);
            //create commit
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"git commit -m 'EnvSnapshot'\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.InternalName}")
                .ExecuteAsync();

            db.Logs.Add("Daemon", "SnapshotCreate - Save hash: " + env.InternalName);
            //save hash
            StringBuilder hash = new();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"git rev-parse HEAD\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.InternalName}")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(hash))
                .ExecuteAsync();

            using (var connection = db.GetConnection())
            {
                //SELECT * FROM environments_snapshots WHERE environments_Id_fk = 1 ORDER BY Created DESC LIMIT 1;
                var Command = new MySqlCommand($"UPDATE environments_snapshots SET Hash = '{hash}' WHERE id = {snap.Id};");
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }

            db.Logs.Add("Daemon", "SnapshotCreate - Enable Site: " + env.InternalName);
            //restart site
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"a2ensite {user.Username}_{env.InternalName}.conf\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service apache2 reload\"")
                .ExecuteAsync();

            db.Environments.SetTaskRunning(env.ID, false);

            db.Logs.Add("Daemon", "SnapshotCreate - Done: " + env.InternalName);

            if (!string.IsNullOrEmpty(user.UserInformation.SlackID))
            {
                var success = await em.SendMessageAsync(string.Format(db.Settings.Get("slack_snapshot_create_finished").Value, env.InternalName)
                    , user.UserInformation.SlackID);
                if (success)
                    return;
            }

            db.Mail.Send($"Snapshot ready for {env.InternalName}!", string.Format(db.Settings.Get("mail_snapshot_create").Value,
                user.Username, env.InternalName), user.Email);
        }
    }
}
