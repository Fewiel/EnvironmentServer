using EnvironmentServer.DAL.Models;
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

        public IEnumerable<EnvironmentSnapshot> GetForEnvironment(long id)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("SELECT * FROM `environments_snapshots` WHERE environments_Id_fk = @id;");
                Command.Parameters.AddWithValue("@id", id);
                Command.Connection = connection;
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
        }

        public IEnumerable<EnvironmentSnapshot> GetTemplates()
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("SELECT * FROM `environments_snapshots` WHERE Template = 1;");
                Command.Connection = connection;
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
        }

        public void CreateSnapshot(string name, long env_id, long user_id)
        {
            DB.CmdAction.CreateTask(new CmdAction() { Action = "snapshot_create", ExecutedById = user_id, Id_Variable = env_id });
        }

        public void DeleteSnapshot(long id)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("DELETE FROM environments_snapshots WHERE id = @id;");
                Command.Parameters.AddWithValue("@id", id);
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }
        }
    }
}
