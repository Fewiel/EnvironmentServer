using EnvironmentServer.DAL.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Repositories
{
    public class SettingsRepository
    {
        private readonly Database DB;

        public SettingsRepository(Database db)
        {
            DB = db;
        }

        public Setting Get(string key)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand($"select * from settings where SettingKey = '{key}';");
                Command.Connection = connection;
                MySqlDataReader reader = Command.ExecuteReader();

                while (reader.Read())
                {
                    var setting = new Setting
                    {
                        ID = reader.GetInt64(0),
                        Key = reader.GetString(1),
                        DisplayName = reader.GetString(2),
                        Value = reader.GetString(3)
                    };

                    reader.Close();
                    return setting;
                }

                reader.Close();
            }

            return null;
        }

        public void Insert(Setting setting)
        {
            DB.Logs.Add("DAL", "Insert setting " + setting.Key);
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("INSERT INTO `settings` (`ID`, `SettingKey`, `DisplayName`, `Value`) VALUES "
                    + "(NULL, @key, @displayName, @value);");
                Command.Parameters.AddWithValue("@key", setting.Key);
                Command.Parameters.AddWithValue("@displayName", setting.DisplayName);
                Command.Parameters.AddWithValue("@value", setting.Value);
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }
        }

        public void Update(Setting setting)
        {
            DB.Logs.Add("DAL", "Update setting " + setting.Key);
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("UPDATE `settings` SET `SettingKey` = @key, `DisplayName` = @displayName, "
                    + "`Value` = @value WHERE `settings`.`ID` = @id;");
                Command.Parameters.AddWithValue("@id", setting.ID);
                Command.Parameters.AddWithValue("@key", setting.Key);
                Command.Parameters.AddWithValue("@displayName", setting.DisplayName);
                Command.Parameters.AddWithValue("@value", setting.Value);
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }
        }

        public void Delete(Setting setting)
        {
            DB.Logs.Add("DAL", "Delete setting " + setting.Key);
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("DELETE FROM `settings` WHERE `settings`.`ID` = @id");
                Command.Parameters.AddWithValue("@id", setting.ID);
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }
        }
    }
}
