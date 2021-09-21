using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Repositories;
using MySql.Data.MySqlClient;

namespace EnvironmentServer.DAL
{
    public class Database
    {
        private readonly string ConnString;

        public SettingsRepository Settings { get; }
        public UsersRepository Users { get; set; }

        public Database(string connString)
        {
            ConnString = connString;
            Settings = new SettingsRepository(this);
            Users = new UsersRepository(this);
            if (Users.GetByUsername("Admin") == null)
            {
                //Task.Run(() => Users.InsertAsync(new User
                //{
                //    Email = "root@root.tld",
                //    Username = "Admin",
                //    Password = PasswordHasher.Hash("Admin"),
                //    IsAdmin = true
                //}, "Admin"));
                Users.InsertAsync(new User
                {
                    Email = "root@root.tld",
                    Username = "Admin",
                    Password = PasswordHasher.Hash("Admin"),
                    IsAdmin = true
                }, "Admin");
            }
        }

        public MySqlConnection GetConnection()
        {
            var c = new MySqlConnection(ConnString);
            c.Open();
            return c;
        }
    }
}
