using CliWrap;
using EnvironmentServer.DAL;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions
{
    public class SnapshotRestoreLatest : ActionBase
    {
        public override string ActionIdentifier => "snapshot_restore_latest";

        public override async Task ExecuteAsync(Database db, long variableID, long userID)
        {
            //Get user, environment and databasename
            var user = db.Users.GetByID(userID);
            var env = db.Environments.Get(variableID);
            var snap = db.Snapshot.GetLatest(variableID);
            var dbString = user.Username + "_" + env.Name;

            using (var connection = db.GetConnection())
            {
                var Command = new MySqlCommand("UPDATE environments_settings_values SET `Value` = 'True' WHERE environments_ID_fk = @envid And environments_settings_ID_fk = 4;");
                Command.Parameters.AddWithValue("@envid", env.ID);
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }

            db.Logs.Add("Daemon", "SnapshotRestoreLatest - Disable Site: " + env.Name);
            //Stop Website (a2dissite)
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"a2dissite {user.Username}_{env.Name}.conf\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service apache2 reload\"")
                .ExecuteAsync();

            db.Logs.Add("Daemon", "SnapshotRestoreLatest - git checkout: " + env.Name);
            //Git checkout
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"git checkout {snap.Hash}\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
                .ExecuteAsync();

            db.Logs.Add("Daemon", "SnapshotRestoreLatest - Recreate/dump database: " + env.Name);
            //Recreate Database            
            using (var connection = db.GetConnection())
            {
                var Command = new MySqlCommand("drop database " + dbString + ";");
                Command.Connection = connection;
                Command.ExecuteNonQuery();

                Command = new MySqlCommand("create database " + dbString + ";");
                Command.Connection = connection;
                Command.ExecuteNonQuery();

                Command = new MySqlCommand("grant all on " + dbString + ".* to '" + user.Username + "'@'localhost';");
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }

            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"mysql -u adm -p1594875!Adm " + dbString + " < db.sql\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
                .ExecuteAsync();

            db.Logs.Add("Daemon", "SnapshotRestoreLatest - Enable Site: " + env.Name);
            //restart site
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"a2ensite {user.Username}_{env.Name}.conf\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service apache2 reload\"")
                .ExecuteAsync();

            using (var connection = db.GetConnection())
            {
                var Command = new MySqlCommand("UPDATE environments_settings_values SET `Value` = 'False' WHERE environments_ID_fk = @envid And environments_settings_ID_fk = 4;");
                Command.Parameters.AddWithValue("@envid", env.ID);
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }
            db.Logs.Add("Daemon", "SnapshotRestoreLatest - Done: " + env.Name);
        }
    }
}
