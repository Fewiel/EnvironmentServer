using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Cms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Repositories
{
    public class EnvironmentSnapshotRepository
    {
        private Database DB;

        public EnvironmentSnapshotRepository(Database database)
        {
            DB = database;
        }

        public EnvironmentSnapshot Get(long id)
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var Command = new MySqlCommand("SELECT * FROM `environments_snapshots` WHERE id = @id;");
            Command.Parameters.AddWithValue("@id", id);
            Command.Connection = c.Connection;
            MySqlDataReader reader = Command.ExecuteReader();
            EnvironmentSnapshot env = null;
            while (reader.Read())
            {
                env = new EnvironmentSnapshot()
                {
                    Id = reader.GetInt64(0),
                    EnvironmentId = reader.GetInt64(1),
                    Name = reader.GetString(2),
                    Hash = reader.GetString(3),
                    Template = reader.GetBoolean(4),
                    Created = reader.GetDateTime(5)
                };
            }
            reader.Close();
            return env;
        }

        public EnvironmentSnapshot GetLatest(long id)
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var Command = new MySqlCommand("SELECT * FROM `environments_snapshots` WHERE environments_Id_fk = @id ORDER BY Created DESC LIMIT 1;");
            Command.Parameters.AddWithValue("@id", id);
            Command.Connection = c.Connection;
            MySqlDataReader reader = Command.ExecuteReader();
            EnvironmentSnapshot env = null;
            while (reader.Read())
            {
                env = new EnvironmentSnapshot()
                {
                    Id = reader.GetInt64(0),
                    EnvironmentId = reader.GetInt64(1),
                    Name = reader.GetString(2),
                    Hash = reader.GetString(3),
                    Template = reader.GetBoolean(4),
                    Created = reader.GetDateTime(5)
                };
            }
            reader.Close();
            return env;
        }

        public IEnumerable<EnvironmentSnapshot> GetForEnvironment(long id)
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var Command = new MySqlCommand("SELECT * FROM `environments_snapshots` WHERE environments_Id_fk = @id;");
            Command.Parameters.AddWithValue("@id", id);
            Command.Connection = c.Connection;
            MySqlDataReader reader = Command.ExecuteReader();
            while (reader.Read())
            {
                yield return new EnvironmentSnapshot()
                {
                    Id = reader.GetInt64(0),
                    EnvironmentId = reader.GetInt64(1),
                    Name = reader.GetString(2),
                    Hash = reader.GetString(3),
                    Template = reader.GetBoolean(4),
                    Created = reader.GetDateTime(5)
                };
            }
            reader.Close();
        }

        public IEnumerable<EnvironmentSnapshot> GetTemplates()
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var Command = new MySqlCommand("SELECT * FROM `environments_snapshots` WHERE Template = 1;");
            Command.Connection = c.Connection;
            MySqlDataReader reader = Command.ExecuteReader();
            var EnvSnap = new EnvironmentSnapshot();
            while (reader.Read())
            {
                yield return new EnvironmentSnapshot()
                {
                    Id = reader.GetInt64(0),
                    EnvironmentId = reader.GetInt64(1),
                    Name = reader.GetString(2),
                    Hash = reader.GetString(3),
                    Template = reader.GetBoolean(4),
                    Created = reader.GetDateTime(5)
                };
            }
            reader.Close();
        }

        public void CreateSnapshot(string name, long env_id, long user_id)
        {
            DB.Logs.Add("DAL", "Create snapshot " + name);
            long id;
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var Command = new MySqlCommand("INSERT INTO `environments_snapshots` (`Id`, `environments_Id_fk`, `Name`, `Hash`, `Template`, `Created`) VALUES (NULL, @envid, @name, '0', '0', NOW());");
            Command.Parameters.AddWithValue("@envid", env_id);
            Command.Parameters.AddWithValue("@name", name);
            Command.Connection = c.Connection;
            Command.ExecuteNonQuery();
            id = Command.LastInsertedId;

            Command = new MySqlCommand("UPDATE environments_settings_values SET `Value` = 'True' WHERE environments_ID_fk = @envid And environments_settings_ID_fk = 4;");
            Command.Parameters.AddWithValue("@envid", env_id);
            Command.Connection = c.Connection;
            Command.ExecuteNonQuery();

            DB.CmdAction.CreateTask(new CmdAction() { Action = "snapshot_create", ExecutedById = user_id, Id_Variable = id });
        }

        public void UpdateSnapshot(EnvironmentSnapshot snapshot)
        {
            DB.Logs.Add("DAL", "Update snapshot " + snapshot.Name);
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var Command = new MySqlCommand("UPDATE `environments_snapshots` SET " +
                    "Name = @name, " +
                    "Hash = @hash, " +
                    "Template = @template " +
                    "WHERE `environments_snapshots`.`Id` = 9;");
            Command.Parameters.AddWithValue("@hash", snapshot.Hash);
            Command.Parameters.AddWithValue("@name", snapshot.Name);
            Command.Parameters.AddWithValue("@template", snapshot.Template);
            Command.Connection = c.Connection;
            Command.ExecuteNonQuery();
        }

        public void DeleteSnapshot(long id)
        {
            DB.Logs.Add("DAL", "Delete snapshot " + id);
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var Command = new MySqlCommand("DELETE FROM environments_snapshots WHERE id = @id;");
            Command.Parameters.AddWithValue("@id", id);
            Command.Connection = c.Connection;
            Command.ExecuteNonQuery();
        }
    }
}