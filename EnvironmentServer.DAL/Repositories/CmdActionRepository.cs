using EnvironmentServer.DAL.Models;
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
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("SELECT * FROM `cmd_actions` WHERE Executed IS NULL LIMIT 1;");
                Command.Connection = connection;
                MySqlDataReader reader = Command.ExecuteReader();
                var cmdaction = new CmdAction();
                while (reader.Read())
                {
                    cmdaction = new CmdAction()
                    {
                        Id = reader.GetInt64(0),
                        Action = reader.GetString(1),
                        Id_Variable = reader.GetInt64(2),
                        Executed = reader.GetDateTime(3),
                        ExecutedById = reader.GetInt64(4)
                    };
                }
                reader.Close();
                return cmdaction;
            }
        }

        public void SetExecuted(long id)
        {
            DB.Logs.Add("DAL", "Completed Task " + id);
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("UPDATE `cmd_actions` SET `Executed` = NOW() WHERE `cmd_actions`.`Id` = @id;");
                Command.Parameters.AddWithValue("@id", id);
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }
        }

        public void CreateTask(CmdAction action)
        {
            DB.Logs.Add("DAL", "Create Task " + action.Action);
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand($"INSERT INTO `cmd_actions` " +
                    $"(`Id`, `Action`, `Id_Variable`, `Executed`, `Executed_By_Id_fk`) " +
                    $"VALUES (NULL, '{action.Action}', '{action.Id_Variable}', NULL, '{action.ExecutedById}');");
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }
        }

        //Delete old entries (7 days back)
        public void DeleteOldExecuted()
        {
            DB.Logs.Add("DAL", "Delete old Tasks");
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("DELETE FROM cmd_actions WHERE Executed <= NOW() - INTERVAL 7 DAY");
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }
        }
    }
}
