using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvironmentServer.DAL.Repositories;
using MySql.Data.MySqlClient;

namespace EnvironmentServer.DAL
{
    public class Database
    {
        private string MySQLServer = "environment.p-weitkamp.de";
        private int MySQLPort = 3306;
        private string MySQLUser = "admin";
        private string MySQLPassword = "1594875";
        private string MySQLDatabase = "EnvironmentServer";
        private MySqlConnection Conn;
        private string ConnString;

        public SettingsRepository Settings { get; }

        public Database(string connString)
        {
            ConnString = connString;
            Settings = new SettingsRepository(this);
        }

        public MySqlConnection GetConnection() => new MySqlConnection(ConnString);
    }
}
