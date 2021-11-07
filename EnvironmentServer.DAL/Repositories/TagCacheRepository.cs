using EnvironmentServer.DAL.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Repositories
{
    public class TagCacheRepository
    {
        private Database DB;

        public TagCacheRepository(Database db)
        {
            DB = db;
        }

        public void CreateIfNotExist(Tag tag)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand($"INSERT IGNORE INTO tag_cache (`Id`, `Name`, `Hash`) " +
                    $"VALUES (NULL, '{tag.Name}', '{tag.Hash}');");
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }
        }

        public IEnumerable<Tag> Get()
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand($"select * from tag_cache;");
                Command.Connection = connection;
                MySqlDataReader reader = Command.ExecuteReader();
                if (reader.Read())
                {
                    yield return new Tag
                    {
                        Name = reader.GetString(1),
                        Hash = reader.GetString(2)
                    };
                }
                reader.Close();
            }
        }
    }
}
