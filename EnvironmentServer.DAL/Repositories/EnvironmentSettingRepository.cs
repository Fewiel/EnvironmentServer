using EnvironmentServer.DAL.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Repositories
{
    public class EnvironmentSettingRepository
    {
        private readonly Database DB;

        public EnvironmentSettingRepository(Database db)
        {
            DB = db;
        }

        public EnvironmentSetting Get(long id)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("select * from environments_settings where ID = @id;");
                Command.Parameters.AddWithValue("@id", id);
                Command.Connection = connection;
                connection.Open();
                MySqlDataReader reader = Command.ExecuteReader();

                while (reader.Read())
                {
                    var environment = FromReader(reader);

                    reader.Close();
                    return environment;
                }

                reader.Close();
            }

            return null;
        }

        private EnvironmentSetting FromReader(MySqlDataReader reader)
        {
            return new EnvironmentSetting
            {
                ID = reader.GetInt64(0),
                Property = reader.GetString(1)
            };
        }

        public void Insert(EnvironmentSetting environment)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("INSERT INTO `environments_settings` (`ID`, `Property`) VALUES "
                    + "(NULL, '@property');");
                Command.Parameters.AddWithValue("@property", environment.Property);
                Command.Connection = connection;
                connection.Open();
                Command.ExecuteNonQuery();
            }
        }

        public void Update(EnvironmentSetting environment)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("UPDATE `environments_settings` SET `Property` = '@property'"
                    + " WHERE `environments_settings`.`ID` = @id;");
                Command.Parameters.AddWithValue("@id", environment.ID);
                Command.Parameters.AddWithValue("@property", environment.Property);
                Command.Connection = connection;
                connection.Open();
                Command.ExecuteNonQuery();
            }
        }

        public void Delete(EnvironmentSetting environment)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("DELETE FROM `environments_settings` WHERE `environments_settings`.`ID` = @id");
                Command.Parameters.AddWithValue("@id", environment.ID);
                Command.Connection = connection;
                connection.Open();
                Command.ExecuteNonQuery();
            }
        }
    }
}
