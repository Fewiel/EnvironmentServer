﻿using CliWrap;
using EnvironmentServer.DAL;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
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
            var snap = db.Snapshot.Get(variableID);
            var env = db.Environments.Get(snap.EnvironmentId);
            var dbString = user.Username + "_" + env.Name;

            using (var connection = db.GetConnection())
            {
                var Command = new MySqlCommand("UPDATE environments_settings_values SET `Value` = 'True' WHERE environments_ID_fk = @envid And environments_settings_ID_fk = 4;");
                Command.Parameters.AddWithValue("@envid", env.ID);
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }

            var url = System.IO.File.ReadAllText($"/home/{user.Username}/files/{env.Name}/dl.txt");

            db.Logs.Add("Daemon", "Download File for: " + env.Name);

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"wget -c {url} -O dl.zip\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"unzip dl.zip\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"rm dl.zip\"")
                .WithWorkingDirectory($"/home/{user.Username}/files/{env.Name}")
                .ExecuteAsync();

            using (var connection = db.GetConnection())
            {
                var Command = new MySqlCommand("UPDATE environments_settings_values SET `Value` = 'False' WHERE environments_ID_fk = @envid And environments_settings_ID_fk = 4;");
                Command.Parameters.AddWithValue("@envid", env.ID);
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }
        }
    }
}