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
    public class SnapshotRestore : ActionBase
    {
        public override string ActionIdentifier => "snapshot_restore";

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

            db.Logs.Add("Daemon", "SnapshotRestore - Disable Site: " + env.InternalName);
            //Stop Website (a2dissite)
            await Bash.ApacheDisableSiteAsync($"{user.Username}_{env.InternalName}.conf");
            await Bash.ReloadApacheAsync();

            await Bash.ChownAsync("root", "root", $"/home/{user.Username}/files/{env.InternalName}", true);

            db.Logs.Add("Daemon", "SnapshotRestore - git reset --hard: " + env.InternalName);
            //Git reset            
            await Bash.CommandAsync($"git reset --hard {snap.Hash}", $"/home/{user.Username}/files/{env.InternalName}");

            //git clean -f -d
            await Bash.CommandAsync("git clean -f -d", $"/home/{user.Username}/files/{env.InternalName}");

            db.Logs.Add("Daemon", "SnapshotRestore - Recreate/dump database: " + env.InternalName);
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


            foreach (var i in db.Snapshot.GetForEnvironment(env.ID))
            {
                if (i.Id > snap.Id)
                {
                    db.Snapshot.DeleteSnapshot(i.Id);
                }
            }

            await Bash.CommandAsync($"mysql -u {config.Username} -p{config.Password} \" + dbString + \" < db.sql",
                $"/home/{user.Username}/files/{env.InternalName}", log: false);

            await Bash.ChownAsync(user.Username, "sftp_users", $"/home/{user.Username}/files/{env.InternalName}", true);

            db.Logs.Add("Daemon", "SnapshotRestore - Enable Site: " + env.InternalName);
            //restart site
            await Bash.ApacheEnableSiteAsync($"{user.Username}_{env.InternalName}.conf");
            await Bash.ReloadApacheAsync();

            db.Environments.SetTaskRunning(env.ID, false);

            db.Logs.Add("Daemon", "SnapshotRestore - Done: " + env.InternalName);

            if (!string.IsNullOrEmpty(user.UserInformation.SlackID))
            {
                var success = await em.SendMessageAsync(string.Format(db.Settings.Get("slack_snapshot_restore_finished").Value, env.InternalName),
                    user.UserInformation.SlackID);
                if (success)
                    return;
            }

            db.Mail.Send($"Snapshot restored for {env.InternalName}!",
                string.Format(db.Settings.Get("mail_snapshot_restored").Value, user.Username, env.InternalName), user.Email);
        }
    }
}
