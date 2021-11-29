using CliWrap;
using Dapper;
using EnvironmentServer.DAL.Enums;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Mail;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Repositories
{
    public class UsersRepository
    {
        private readonly Database DB;
        private const string phpfpm = @"
[{0}]
user = {0}
group = sftp_users

listen = /run/php/{1}-{0}.sock

listen.owner = {0}
listen.group = sftp_users

pm = dynamic
pm.max_children = 5
pm.start_servers = 2
pm.min_spare_servers = 1
pm.max_spare_servers = 3

php_admin_value[open_basedir] = /home/{0}/files
php_admin_value[sys_temp_dir] = /home/{0}/files/php/tmp
php_admin_value[upload_tmp_dir] = /home/{0}/files/php/tmp";
        private static readonly Random Random = new();
        public static string RandomPasswordString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[Random.Next(s.Length)]).ToArray());
        }

        public UsersRepository(Database db)
        {
            DB = db;
        }

        public IEnumerable<User> GetUsers()
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("select * from users;");
                Command.Connection = connection;
                MySqlDataReader reader = Command.ExecuteReader();

                while (reader.Read())
                {
                    yield return new User
                    {
                        ID = reader.GetInt64(0),
                        Email = reader.GetString(1),
                        Username = reader.GetString(2),
                        Password = reader.GetString(3),
                        IsAdmin = reader.GetBoolean(4)
                    };
                }
                reader.Close();
            }
        }

        public User GetByID(long ID)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("select * from users where ID = @id;");
                Command.Parameters.AddWithValue("@id", ID);
                Command.Connection = connection;
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

        public async Task InsertAsync(User user, string shellPassword)
        {
            DB.Logs.Add("DAL", "Insert user " + user.Username);
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("INSERT INTO `users` (`ID`, `Email`, `Username`, `Password`, `IsAdmin`) "
                     + "VALUES (NULL, @email, @username, @password, @isAdmin)");
                Command.Parameters.AddWithValue("@email", user.Email);
                Command.Parameters.AddWithValue("@username", user.Username);
                Command.Parameters.AddWithValue("@password", user.Password);
                Command.Parameters.AddWithValue("@isAdmin", user.IsAdmin);
                Command.Connection = connection;
                Command.ExecuteNonQuery();

                Command = new MySqlCommand("create user '" + user.Username + "'@'localhost' identified by @password;");
                Command.Parameters.AddWithValue("@password", shellPassword);
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }

            DB.Logs.Add("DAL", "Start Useradd: " + user.Username);

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"useradd -p $(openssl passwd -1 {shellPassword}) {user.Username}\"")                
                .ExecuteAsync();     
            
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"usermod -G sftp_users {user.Username}\"")
                .ExecuteAsync();
            
            DB.Logs.Add("DAL", "Create user homefolder: " + user.Username);
            Directory.CreateDirectory($"/home/{user.Username}");

            await Cli.Wrap("/bin/bash")
               .WithArguments($"-c \"chown root /home/{user.Username}\"")
               .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"chmod 755 /home/{user.Username}\"")
                .ExecuteAsync();

            DB.Logs.Add("DAL", "Create user files folder: " + user.Username);
            Directory.CreateDirectory($"/home/{user.Username}/files");
            Directory.CreateDirectory($"/home/{user.Username}/files/php");
            Directory.CreateDirectory($"/home/{user.Username}/files/php/tmp");
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"chown -R {user.Username} /home/{user.Username}/files\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"chmod 755 /home/{user.Username}/files\"")
                .ExecuteAsync();

            DB.Logs.Add("DAL", "Create user php-fpm - " + user.Username);
            var conf = string.Format(phpfpm, user.Username, "php5.6-fpm");
            File.WriteAllText($"/etc/php/5.6/fpm/pool.d/{user.Username}.conf", conf);
            conf = string.Format(phpfpm, user.Username, "php7.2-fpm");
            File.WriteAllText($"/etc/php/7.2/fpm/pool.d/{user.Username}.conf", conf);
            conf = string.Format(phpfpm, user.Username, "php7.4-fpm");
            File.WriteAllText($"/etc/php/7.4/fpm/pool.d/{user.Username}.conf", conf);
            conf = string.Format(phpfpm, user.Username, "php8.0-fpm");
            File.WriteAllText($"/etc/php/8.0/fpm/pool.d/{user.Username}.conf", conf);
            conf = string.Format(phpfpm, user.Username, "php8.1-fpm");
            File.WriteAllText($"/etc/php/8.1/fpm/pool.d/{user.Username}.conf", conf);

            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service php5.6-fpm reload\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service php7.2-fpm reload\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service php7.4-fpm reload\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service php8.0-fpm reload\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service php8.1-fpm reload\"")
                .ExecuteAsync();
            DB.Mail.Send("Shopware Environment Server Account",
                string.Format(DB.Settings.Get("mail_account_created").Value, user.Username, shellPassword), user.Email);
            DB.Logs.Add("DAL", "New User added: " + user.Username);
        }

        public async Task RegenerateConfig()
        {

            foreach (var user in DB.Users.GetUsers())
            {
                var conf = string.Format(phpfpm, user.Username, "php5.6-fpm");
                File.WriteAllText($"/etc/php/5.6/fpm/pool.d/{user.Username}.conf", conf);
                conf = string.Format(phpfpm, user.Username, "php7.2-fpm");
                File.WriteAllText($"/etc/php/7.2/fpm/pool.d/{user.Username}.conf", conf);
                conf = string.Format(phpfpm, user.Username, "php7.4-fpm");
                File.WriteAllText($"/etc/php/7.4/fpm/pool.d/{user.Username}.conf", conf);
                conf = string.Format(phpfpm, user.Username, "php8.0-fpm");
                File.WriteAllText($"/etc/php/8.0/fpm/pool.d/{user.Username}.conf", conf);
                conf = string.Format(phpfpm, user.Username, "php8.1-fpm");
                File.WriteAllText($"/etc/php/8.1/fpm/pool.d/{user.Username}.conf", conf);

                foreach (var env in DB.Environments.GetForUser(user.ID))
                {
                    await DB.Environments.UpdatePhpAsync(env.ID, user, env.Version);
                }

            }           

            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service php5.6-fpm reload\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service php7.2-fpm reload\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service php7.4-fpm reload\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service php8.0-fpm reload\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service php8.1-fpm reload\"")
                .ExecuteAsync();

            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service apache2 reload\"")
                .ExecuteAsync();

        }

        public async Task UpdateAsync(User user, string shellPassword)
        {
            DB.Logs.Add("DAL", "Update user " + user.Username);
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
                Command.ExecuteNonQuery();
            }

            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand($"ALTER USER '{user.Username}'@'localhost' IDENTIFIED BY @password;");
                Command.Parameters.AddWithValue("@password", shellPassword);
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }

            using (var connection = DB.GetConnection())
            {
                connection.Execute("UPDATE mysql.user SET Super_Priv='Y' WHERE user=@user;",
                    new {
                        user = user.Username
                    });
            }

            using (var connection = DB.GetConnection())
            {
                connection.Execute("FLUSH PRIVILEGES;");
            }

            await Cli.Wrap("/bin/bash")
            .WithArguments($"-c \"echo \'{user.Username}:{shellPassword}\' | sudo chpasswd\"")
            .ExecuteAsync();
        }

        public async Task UpdateByAdminAsync(User usr, bool newPassword)
        {
            DB.Logs.Add("DAL", "Admin Update user " + usr.Username);

            var user = new User();

            if (newPassword)
            {
                var shellPassword = RandomPasswordString(32);

                user = new User
                {
                    ID = usr.ID,
                    Username = usr.Username,
                    Email = usr.Email,
                    IsAdmin = usr.IsAdmin,
                    Password = PasswordHasher.Hash(shellPassword)
                };

                using (var connection = DB.GetConnection())
                {
                    var Command = new MySqlCommand($"ALTER USER '{user.Username}'@'localhost' IDENTIFIED BY @password;");
                    Command.Parameters.AddWithValue("@password", shellPassword);
                    Command.Connection = connection;
                    Command.ExecuteNonQuery();
                }

                await Cli.Wrap("/bin/bash")
                    .WithArguments($"-c \"echo \'{user.Username}:{shellPassword}\' | sudo chpasswd\"")
                    .ExecuteAsync();

                DB.Mail.Send("Password reseted", string.Format(DB.Settings.Get("mail_account_password").Value, usr.Username, shellPassword), usr.Email);
            }
            else
            {
                user = usr;
            }

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
                Command.ExecuteNonQuery();
            }

            DB.Mail.Send("Account updated", string.Format(DB.Settings.Get("mail_account_update").Value, user.Username, user.Email, user.IsAdmin), user.Email);
        }

        public async Task LockUserAsync(User usr)
        {
            DB.Logs.Add("DAL", "Lock user " + usr.Username);

            var user = usr;

            var shellPassword = RandomPasswordString(32);

            user.Password = shellPassword;

            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand($"ALTER USER '{user.Username}'@'localhost' IDENTIFIED BY @password;");
                Command.Parameters.AddWithValue("@password", shellPassword);
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"echo \'{user.Username}:{shellPassword}\' | sudo chpasswd\"")
                .ExecuteAsync();

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
                Command.ExecuteNonQuery();
            }
        }

        public async Task DeleteAsync(User user)
        {
            foreach (var env in DB.Environments.GetForUser(user.ID))
                await DB.Environments.DeleteAsync(env, user).ConfigureAwait(false);

            DB.Logs.Add("DAL", "Delete user php-fpm - " + user.Username);
            File.Delete($"/etc/php/5.6/fpm/pool.d/{user.Username}.conf");
            File.Delete($"/etc/php/7.2/fpm/pool.d/{user.Username}.conf");
            File.Delete($"/etc/php/7.4/fpm/pool.d/{user.Username}.conf");
            File.Delete($"/etc/php/8.0/fpm/pool.d/{user.Username}.conf");
            File.Delete($"/etc/php/8.1/fpm/pool.d/{user.Username}.conf");

            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service php5.6-fpm reload\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service php7.2-fpm reload\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service php7.4-fpm reload\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service php8.0-fpm reload\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service php8.1-fpm reload\"")
                .ExecuteAsync();

            DB.Logs.Add("DAL", "Delete user " + user.Username);
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("DELETE FROM `users` WHERE `users`.`ID` = @id");
                Command.Parameters.AddWithValue("@id", user.ID);
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand($"DROP USER '{user.Username}'@'localhost';");
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"userdel {user.Username} --force\"")
                .ExecuteAsync();

            Directory.Delete($"/home/{user.Username}", true);
            DB.Logs.Add("DAL", "Delete user complete for " + user.Username);
        }
    }
}
