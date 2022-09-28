﻿using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Utility;
using EnvironmentServer.Interfaces;
using EnvironmentServer.Util;
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
            await Bash.ApacheDisableSiteAsync($"{user.Username}_{env.InternalName}.conf");
            await Bash.ReloadApacheAsync();
            
            db.Logs.Add("Daemon", "SnapshotCreate - Create database dump: " + env.InternalName);
            //Create database dump in site folder
            await Bash.CommandAsync($"mysqldump -u {{config.Username}} -p{{config.Password}} \" + dbString + \" > db.sql",
                $"/home/{user.Username}/files/{env.InternalName}");

            await Bash.ChownAsync("root", "root", $"/home/{user.Username}/files/{env.InternalName}", true);

            db.Logs.Add("Daemon", "SnapshotCreate - Git init: " + env.InternalName);
            //check for git init
            await Bash.CommandAsync("git init", $"/home/{user.Username}/files/{env.InternalName}");

            db.Logs.Add("Daemon", "SnapshotCreate - Git create: " + env.InternalName);
            //git stage
            await Bash.CommandAsync("git stage -A", $"/home/{user.Username}/files/{env.InternalName}");

            db.Logs.Add("Daemon", "SnapshotCreate - Git commit: " + env.InternalName);
            //create commit
            await Bash.CommandAsync("git commit -m 'EnvSnapshot'", $"/home/{user.Username}/files/{env.InternalName}");

            db.Logs.Add("Daemon", "SnapshotCreate - Save hash: " + env.InternalName);
            //save hash
            StringBuilder hash = await Bash.CommandQueryAsync("git rev-parse HEAD", $"/home/{user.Username}/files/{env.InternalName}");
            
            using var c = new MySQLConnectionWrapper(db.ConnString);
            //SELECT * FROM environments_snapshots WHERE environments_Id_fk = 1 ORDER BY Created DESC LIMIT 1;
            var Command = new MySqlCommand($"UPDATE environments_snapshots SET Hash = '{hash}' WHERE id = {snap.Id};");
            Command.Connection = c.Connection;
            Command.ExecuteNonQuery();

            await Bash.ChownAsync(user.Username, "sfpt_users", $"/home/{user.Username}/files/{env.InternalName}", true);

            db.Logs.Add("Daemon", "SnapshotCreate - Enable Site: " + env.InternalName);
            //restart site
            await Bash.ApacheEnableSiteAsync($"{user.Username}_{env.InternalName}.conf");
            await Bash.ReloadApacheAsync();

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
