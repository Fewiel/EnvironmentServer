using EnvironmentServer.DAL.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Repositories
{
    public class UsersRepository
    {
        private readonly Database DB;

        public UsersRepository(Database db)
        {
            DB = db;
        }

        public User GetByID(long ID)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("select * from users where ID = @id;");
                Command.Parameters.AddWithValue("@id", ID);
                Command.Connection = connection;
                connection.Open();
                MySqlDataReader reader = Command.ExecuteReader();

                while (reader.Read())
                {
                    var setting = new User
                    {
                        ID = reader.GetInt64(0),
                        Email = reader.GetString(1),
                        Username = reader.GetString(2),
                        Password = reader.GetString(3),
                        IsAdmin = reader.GetBoolean(4)
                    };

                    reader.Close();
                    return setting;
                }

                reader.Close();
            }

            return null;
        }

        public User GetByUsername(string username)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("select * from users where Username = @username;");
                Command.Parameters.AddWithValue("@username", username);
                Command.Connection = connection;
                connection.Open();
                MySqlDataReader reader = Command.ExecuteReader();

                while (reader.Read())
                {
                    var setting = new User
                    {
                        ID = reader.GetInt64(0),
                        Email = reader.GetString(1),
                        Username = reader.GetString(2),
                        Password = reader.GetString(3),
                        IsAdmin = reader.GetBoolean(4)
                    };

                    reader.Close();
                    return setting;
                }

                reader.Close();
            }

            return null;
        }

        public void Insert(User user)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("INSERT INTO `users` (`ID`, `Email`, `Username`, `Password`, `IsAdmin`) "
                     + "VALUES (NULL, @email, @username, @password, @isAdmin)");
                Command.Parameters.AddWithValue("@email", user.Email);
                Command.Parameters.AddWithValue("@username", user.Username);
                Command.Parameters.AddWithValue("@password", user.Password);
                Command.Parameters.AddWithValue("@isAdmin", user.IsAdmin);
                Command.Connection = connection;
                connection.Open();
                Command.ExecuteNonQuery();
            }
        }

        public void Update(User user)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("UPDATE `users` SET "
                     + "`Email` = @email, `Username` = @username, `Password` = @password, `IsAdmin` = @isAdmin WHERE `users`.`ID` = @id");
                Command.Parameters.AddWithValue("@id", user.ID);
                Command.Parameters.AddWithValue("@email", user.Email);
                Command.Parameters.AddWithValue("@username", user.Username);
                Command.Parameters.AddWithValue("@password", user.Password);
                Command.Parameters.AddWithValue("@isAdmin", user.IsAdmin);
                Command.Connection = connection;
                connection.Open();
                Command.ExecuteNonQuery();
            }
        }

        public void Delete(User user)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("DELETE FROM `users` WHERE `users`.`ID` = @id");
                Command.Parameters.AddWithValue("@id", user.ID);
                Command.Connection = connection;
                connection.Open();
                Command.ExecuteNonQuery();
            }
        }
    }
}
