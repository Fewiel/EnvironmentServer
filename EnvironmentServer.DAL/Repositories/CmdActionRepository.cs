using Dapper;
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
    public class CmdActionRepository
    {
        private Database DB;
        public CmdActionRepository(Database database)
        {
            DB = database;
        }

        public CmdAction GetFirstNonExecuted()
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);

            var Command = new MySqlCommand("SELECT * FROM `cmd_actions` WHERE Executed IS NULL LIMIT 1;");
            Command.Connection = c.Connection;
            MySqlDataReader reader = Command.ExecuteReader();
            var cmdaction = new CmdAction();
            while (reader.Read())
            {
                cmdaction = new CmdAction()
                {
                    Id = reader.GetInt64(0),
                    Action = reader.GetString(1),
                    Id_Variable = reader.GetInt64(2),
                    ExecutedById = reader.GetInt64(4)
                };
            }
            reader.Close();
            return cmdaction;
        }

        public bool Exists(string task, long varID)
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            return c.Connection.ExecuteScalar<bool>("select Count(1) from `cmd_actions` " +
                "where `Action` = @task and `Id_Variable` = @varID and `Executed` <= NOW() - INTERVAL 1 DAY Limit 1;", new
                {
                    task,
                    varID
                });
        }

        public void SetExecuted(long id, string name, long uid)
        {
            DB.Logs.Add("DAL", "Completed Task " + id + " - " + name + " Executed by User ID: " + uid);
            using var c = new MySQLConnectionWrapper(DB.ConnString);

            var Command = new MySqlCommand("UPDATE `cmd_actions` SET `Executed` = NOW() WHERE `cmd_actions`.`Id` = @id;");
            Command.Parameters.AddWithValue("@id", id);
            Command.Connection = c.Connection;
            Command.ExecuteNonQuery();
        }

        public void CreateTask(CmdAction action)
        {
            DB.Logs.Add("DAL", "Create Task " + action.Action);
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var Command = new MySqlCommand($"INSERT INTO `cmd_actions` " +
                $"(`Id`, `Action`, `Id_Variable`, `Executed`, `Executed_By_Id_fk`) " +
                $"VALUES (NULL, '{action.Action}', '{action.Id_Variable}', NULL, '{action.ExecutedById}');");
            Command.Connection = c.Connection;
            Command.ExecuteNonQuery();
        }

        //Delete old entries (7 days back)
        public void DeleteOldExecuted()
        {
            DB.Logs.Add("DAL", "Delete old Tasks");
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var Command = new MySqlCommand("DELETE FROM cmd_actions WHERE Executed <= NOW() - INTERVAL 7 DAY");
            Command.Connection = c.Connection;
            Command.ExecuteNonQuery();
        }
    }
}
