using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Repositories
{
    public class LogRepository
    {
        private Database DB;

        public LogRepository(Database database)
        {
            DB = database;
        }

        public async Task<IEnumerable<Log>> Get()
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            return await c.Connection.QueryAsync<Log>("select * from `logs`");
        }

        public void Add(string source, string message)
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var Command = new MySqlCommand("INSERT INTO `logs` (`Id`, `Source`, `Message`, `Timestamp`) VALUES (NULL, @src, @msg, NOW());");
            Command.Parameters.AddWithValue("@msg", message);
            Command.Parameters.AddWithValue("@src", source);
            Command.Connection = c.Connection;
            Command.ExecuteNonQuery();
        }

        public void DeleteOld()
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            c.Connection.Execute($"DELETE FROM `logs` where Timestamp < DATE(DATE_SUB(NOW(), INTERVAL 30 DAY));");
        }
    }
}
