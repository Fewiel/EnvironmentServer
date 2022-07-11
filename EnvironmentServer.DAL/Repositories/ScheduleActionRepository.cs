using EnvironmentServer.DAL.Enums;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

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
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var Command = new MySqlCommand($"select * from schedule_actions where `Interval` > " + (includeDisabled ? "-2;" : "-1;"));
            Command.Connection = c.Connection;
            MySqlDataReader reader = Command.ExecuteReader();
            while (reader.Read())
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

        public void CreateIfNotExist(ScheduleAction action)
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var Command = new MySqlCommand($"select * from schedule_actions where Action = '{action.Action}';");
            Command.Connection = c.Connection;
            MySqlDataReader reader = Command.ExecuteReader();
            if (reader.Read())
            {
                reader.Close();
                return;
            }
            reader.Close();

            Command = new MySqlCommand($"INSERT INTO `schedule_actions` (`Action`, `Timing`, `Interval`, `LastExecuted`) " +
                $"VALUES ('{action.Action}', {(int)action.Timing}, {action.Interval}, @time);");
            Command.Parameters.AddWithValue("@time", DateTime.MinValue);
            Command.Connection = c.Connection;
            Command.ExecuteNonQuery();
        }

        public void Update(ScheduleAction action)
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var Command = new MySqlCommand($"UPDATE `schedule_actions` SET `Timing` = {(int)action.Timing}, `Interval` = {action.Interval}, " +
                    $"`LastExecuted` = {action.LastExecuted} WHERE `schedule_actions`.`Action` = {action.Action};");
            Command.Connection = c.Connection;
            Command.ExecuteNonQuery();
        }

        public void SetExecuted(long id, DateTime time)
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var Command = new MySqlCommand("UPDATE `schedule_actions` SET `LastExecuted` = @time WHERE `schedule_actions`.`Id` = @id;");
                Command.Parameters.AddWithValue("@time", time);
                Command.Parameters.AddWithValue("@id", id);
                Command.Connection = c.Connection;
                Command.ExecuteNonQuery();
        }
    }
}