using EnvironmentServer.DAL.Models;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Repositories
{
    public class EnvironmentRepository
    {
        private readonly Database DB;

        public EnvironmentRepository(Database db)
        {
            DB = db;
        }

        public Environment Get(string id)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("select * from environments where ID = @id;");
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

        public IEnumerable<Environment> GetForUser(string userID)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("select * from environments where users_ID_fk = @id;");
                Command.Parameters.AddWithValue("@id", userID);
                Command.Connection = connection;
                connection.Open();
                MySqlDataReader reader = Command.ExecuteReader();

                while (reader.Read())
                    yield return FromReader(reader);

                reader.Close();
            }
        }

        private Environment FromReader(MySqlDataReader reader)
        {
            return new Environment
            {
                ID = reader.GetInt64(0),
                UserID = reader.GetInt64(1),
                Name = reader.GetString(2),
                Address = reader.GetString(3)
            };
        }

        public void Insert(Environment environment)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("INSERT INTO `environments` (`ID`, `users_ID_fk`, `Name`, `Address`) VALUES "
                    + "(NULL, '@userID', '@name', '@address');");
                Command.Parameters.AddWithValue("@userID", environment.UserID);
                Command.Parameters.AddWithValue("@name", environment.Name);
                Command.Parameters.AddWithValue("@address", environment.Address);
                Command.Connection = connection;
                connection.Open();
                Command.ExecuteNonQuery();
            }
        }

        public void Update(Environment environment)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("UPDATE `environments` SET `users_ID_fk` = '@userID', `Name` = '@name', "
                    + "`Address` = '@address' WHERE `environments`.`ID` = @id;");
                Command.Parameters.AddWithValue("@id", environment.ID);
                Command.Parameters.AddWithValue("@userID", environment.UserID);
                Command.Parameters.AddWithValue("@name", environment.Name);
                Command.Parameters.AddWithValue("@address", environment.Address);
                Command.Connection = connection;
                connection.Open();
                Command.ExecuteNonQuery();
            }
        }

        public void Delete(Environment environment)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("DELETE FROM `environments` WHERE `environments`.`ID` = @id");
                Command.Parameters.AddWithValue("@id", environment.ID);
                Command.Connection = connection;
                connection.Open();
                Command.ExecuteNonQuery();
            }
        }

    }
}
