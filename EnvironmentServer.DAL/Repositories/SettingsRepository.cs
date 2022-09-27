using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
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
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var Command = new MySqlCommand($"select * from settings where SettingKey = '{key}';");
            Command.Connection = c.Connection;
            MySqlDataReader reader = Command.ExecuteReader();

            while (reader.Read())
            {
                var setting = new Setting
                {
                    ID = reader.GetInt64(0),
                    Key = reader.GetString(1),
                    DisplayName = reader.GetString(2),
                    Value = reader.GetString(3),
                    DisplayType = reader.GetString(4)
                };

                reader.Close();
                return setting;
            }

            reader.Close();
            DB.Logs.Add("SettingsRepository", $"Setting {key} not found");
            return null;
        }

        public IEnumerable<Setting> GetAll()
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var Command = new MySqlCommand($"select * from settings;");
            Command.Connection = c.Connection;
            MySqlDataReader reader = Command.ExecuteReader();

            while (reader.Read())
            {
                yield return new Setting
                {
                    ID = reader.GetInt64(0),
                    Key = reader.GetString(1),
                    DisplayName = reader.GetString(2),
                    Value = reader.GetString(3),
                    DisplayType = reader.GetString(4)
                };
            }
            reader.Close();
        }

        public void Insert(Setting setting)
        {
            DB.Logs.Add("DAL", "Insert setting " + setting.Key);
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var Command = new MySqlCommand("INSERT INTO `settings` (`ID`, `SettingKey`, `DisplayName`, `Value`) VALUES "
                    + "(NULL, @key, @displayName, @value);");
            Command.Parameters.AddWithValue("@key", setting.Key);
            Command.Parameters.AddWithValue("@displayName", setting.DisplayName);
            Command.Parameters.AddWithValue("@value", setting.Value);
            Command.Connection = c.Connection;
            Command.ExecuteNonQuery();
        }

        public void Update(Setting setting)
        {
            DB.Logs.Add("DAL", "Update setting " + setting.Key);
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var Command = new MySqlCommand("UPDATE `settings` SET `SettingKey` = @key, `DisplayName` = @displayName, "
                    + "`Value` = @value WHERE `settings`.`ID` = @id;");
            Command.Parameters.AddWithValue("@id", setting.ID);
            Command.Parameters.AddWithValue("@key", setting.Key);
            Command.Parameters.AddWithValue("@displayName", setting.DisplayName);
            Command.Parameters.AddWithValue("@value", setting.Value);
            Command.Connection = c.Connection;
            Command.ExecuteNonQuery();
        }

        public void Delete(Setting setting)
        {
            DB.Logs.Add("DAL", "Delete setting " + setting.Key);
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var Command = new MySqlCommand("DELETE FROM `settings` WHERE `settings`.`ID` = @id");
            Command.Parameters.AddWithValue("@id", setting.ID);
            Command.Connection = c.Connection;
            Command.ExecuteNonQuery();
        }
    }
}
