using CliWrap;
using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Utility;
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
    public class SnapshotRestoreLatest : ActionBase
    {
        public override string ActionIdentifier => "snapshot_restore_latest";

        public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
        {
            var db = sp.GetService<Database>();
            var em = sp.GetService<IExternalMessaging>();

            //Get user, environment and databasename
            var user = db.Users.GetByID(userID);
            var env = db.Environments.Get(variableID);
            var snap = db.Snapshot.GetLatest(variableID);
            var dbString = user.Username + "_" + env.InternalName;
            var config = JsonConvert.DeserializeObject<DBConfig>(File.ReadAllText("DBConfig.json"));

            db.Logs.Add("Daemon", "SnapshotRestoreLatest - Disable Site: " + env.InternalName);
            //Stop Website (a2dissite)
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"a2dissite {user.Username}_{env.InternalName}.conf\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service apache2 reload\"")
                .ExecuteAsync();

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"chown -R root:root /home/{user.Username}/files/{env.InternalName}\"")
                .ExecuteAsync();

            db.Logs.Add("Daemon", "SnapshotRestoreLatest - git checkout: " + env.InternalName);
            //Git checkout
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"git checkout {snap.Hash}\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.InternalName}")
                .ExecuteAsync();

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"git clean -f -d\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.InternalName}")
                .ExecuteAsync();

            db.Logs.Add("Daemon", "SnapshotRestoreLatest - Recreate/dump database: " + env.InternalName);
            //Recreate Database            
            using var c = new MySQLConnectionWrapper(db.ConnString);
            var Command = new MySqlCommand("drop database " + dbString + ";");
            Command.Connection = c.Connection;
            Command.ExecuteNonQuery();

            Command = new MySqlCommand("create database " + dbString + ";");
            Command.Connection = c.Connection;
            Command.ExecuteNonQuery();

            Command = new MySqlCommand("grant all on " + dbString + ".* to '" + user.Username + "'@'localhost';");
            Command.Connection = c.Connection;
            Command.ExecuteNonQuery();

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"mysql -u {config.Username} -p{config.Password} " + dbString + " < db.sql\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.InternalName}")
                .ExecuteAsync();

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"chown -R {user.Username} /home/{user.Username}/files/{env.InternalName}\"")
                .ExecuteAsync();

            db.Logs.Add("Daemon", "SnapshotRestoreLatest - Enable Site: " + env.InternalName);
            //restart site
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"a2ensite {user.Username}_{env.InternalName}.conf\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service apache2 reload\"")
                .ExecuteAsync();

            db.Environments.SetTaskRunning(env.ID, false);

            db.Logs.Add("Daemon", "SnapshotRestoreLatest - Done: " + env.InternalName);

            if (!string.IsNullOrEmpty(user.UserInformation.SlackID))
            {
                var success = await em.SendMessageAsync(string.Format(db.Settings.Get("slack_snapshot_restore_finished").Value, env.InternalName),
                    user.UserInformation.SlackID);
                if (success)
                    return;
            }

            db.Mail.Send($"Latest Snapshot restored for {env.InternalName}!",
                string.Format(db.Settings.Get("mail_snapshot_restored_latest").Value, user.Username, env.InternalName), user.Email);
        }
    }
}
