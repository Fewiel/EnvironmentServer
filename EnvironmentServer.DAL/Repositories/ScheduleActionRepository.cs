using EnvironmentServer.DAL.Enums;
using EnvironmentServer.DAL.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Repositories
{
    public class ScheduleActionRepository
    {
        private Database DB;

        public ScheduleActionRepository(Database db)
        {
            DB = db;
        }

        public IEnumerable<ScheduleAction> Get(bool includeDisabled)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand($"select * from schedule_actions where `Interval` > " + (includeDisabled ? "-2;" : "-1;"));
                Command.Connection = connection;
                MySqlDataReader reader = Command.ExecuteReader();
                if (reader.Read())
                {
                    yield return new ScheduleAction
                    {
                        Id = reader.GetInt64(0),
                        Action = reader.GetString(1),
                        Timing = (Timing)reader.GetInt32(2),
                        Interval = reader.GetInt32(3),
                        LastExecuted = reader.GetDateTime(4)
                    };
                }
                reader.Close();
            }
        }

        public void CreateIfNotExist(ScheduleAction action)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand($"select * from schedule_actions where Action = '{action.Action}';");
                Command.Connection = connection;
                MySqlDataReader reader = Command.ExecuteReader();
                if (reader.Read())
                {
                    reader.Close();
                    return;
                }
                reader.Close();

                Command = new MySqlCommand($"INSERT INTO `schedule_actions` (`Action`, `Timing`, `Interval`) " +
                    $"VALUES ('{action.Action}', {(int)action.Timing}, {action.Interval});");
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }
        }

        public void Update(ScheduleAction action)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand($"UPDATE `schedule_actions` SET `Timing` = {(int)action.Timing}, `Interval` = {action.Interval}, " +
                    $"`LastExecuted` = {action.LastExecuted} WHERE `schedule_actions`.`Action` = {action.Action};");
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }
        }

        public void SetExecuted(long id, DateTime time)
        {
            DB.Logs.Add("DAL", "Completed Task " + id);
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("UPDATE `schedule_actions` SET `LastExecuted` = @time WHERE `schedule_actions`.`Id` = @id;");
                Command.Parameters.AddWithValue("@time", time);
                Command.Parameters.AddWithValue("@id", id);
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }
        }

    }
}
