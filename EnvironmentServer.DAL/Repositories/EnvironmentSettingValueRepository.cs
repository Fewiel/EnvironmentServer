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
    public class EnvironmentSettingValueRepository
    {
        private readonly Database DB;

        public EnvironmentSettingValueRepository(Database db)
        {
            DB = db;
        }

        public IEnumerable<EnvironmentSettingValue> GetForEnvironment(long environmentID)
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var Command = new MySqlCommand("select * from environments_settings_value where environments_ID_fk = @id;");
            Command.Parameters.AddWithValue("@id", environmentID);
            Command.Connection = c.Connection;
            MySqlDataReader reader = Command.ExecuteReader();

            while (reader.Read())
                yield return FromReader(reader);

            reader.Close();
        }

        public IEnumerable<EnvironmentSettingValue> GetForEnvironmentSetting(long environmentSettingID)
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var Command = new MySqlCommand("select * from environments_settings_value where environments_settings_ID_fk = @id;");
            Command.Parameters.AddWithValue("@id", environmentSettingID);
            Command.Connection = c.Connection;
            MySqlDataReader reader = Command.ExecuteReader();

            while (reader.Read())
                yield return FromReader(reader);

            reader.Close();
        }

        private EnvironmentSettingValue FromReader(MySqlDataReader reader)
        {
            return new EnvironmentSettingValue
            {
                EnvironmentID = reader.GetInt64(0),
                EnvironmentSettingID = reader.GetInt64(1),
                Value = reader.GetString(2)
            };
        }

        public void Insert(EnvironmentSettingValue environment)
        {
            DB.Logs.Add("DAL", "Insert EnvironmentSettingValue");
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var Command = new MySqlCommand("INSERT INTO `environments_settings_values` (`environments_ID_fk`, "
                     + "`environments_settings_ID_fk`, `Value`) VALUES (@environmentID, @environmentSettingID, @value);");
            Command.Parameters.AddWithValue("@environmentID", environment.EnvironmentID);
            Command.Parameters.AddWithValue("@environmentSettingID", environment.EnvironmentSettingID);
            Command.Parameters.AddWithValue("@value", environment.Value);
            Command.Connection = c.Connection;
            Command.ExecuteNonQuery();
        }

        public void Update(EnvironmentSettingValue environment)
        {
            DB.Logs.Add("DAL", "Update EnvironmentSettingValue " + environment.EnvironmentSettingID);
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var Command = new MySqlCommand("UPDATE `environments_settings_values` SET `environments_ID_fk`= @environmentID,"
                 + "`environments_settings_ID_fk`= @environmentSettingID,`Value`=@value WHERE `environments_ID_fk` = @environmentID "
                  + "AND `environments_settings_ID_fk` = @environmentSettingID;");
            Command.Parameters.AddWithValue("@environmentID", environment.EnvironmentID);
            Command.Parameters.AddWithValue("@environmentSettingID", environment.EnvironmentSettingID);
            Command.Parameters.AddWithValue("@value", environment.Value);
            Command.Connection = c.Connection;
            Command.ExecuteNonQuery();
        }

        public void Delete(EnvironmentSettingValue environment)
        {
            DB.Logs.Add("DAL", "Delete EnvironmentSettingValue " + environment.EnvironmentSettingID);
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var Command = new MySqlCommand("DELETE FROM `environments_settings_values` WHERE `environments_ID_fk` = @environmentID "
                      + "AND `environments_settings_ID_fk` = @environmentSettingID;");
                Command.Parameters.AddWithValue("@environmentID", environment.EnvironmentID);
                Command.Parameters.AddWithValue("@environmentSettingID", environment.EnvironmentSettingID);
                Command.Connection = c.Connection;
                Command.ExecuteNonQuery();
        }
    }
}