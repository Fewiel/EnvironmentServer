using CliWrap;
using EnvironmentServer.DAL.Models;
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

        public void InsertAsync(User user, string shellPassword)
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
                Command.ExecuteNonQuery();

                Command = new MySqlCommand("create user '" + user.Username + "'@'localhost' identified by @password;");
                Command.Parameters.AddWithValue("@password", shellPassword);
                Command.Connection = connection;
                Command.ExecuteNonQuery();

            }

            //sudo addgroup sftp_users

            //  /etc/ssh/sshd_config
            //https://linuxize.com/post/how-to-set-up-sftp-chroot-jail/
            //
            //            Match Group sftp_users
            //# Force the connection to use SFTP and chroot to the required directory.
            //              ForceCommand internal-sftp
            //              ChrootDirectory %h
            //              # Disable tunneling, authentication agent, TCP and X11 forwarding.
            //              PermitTunnel no
            //              AllowAgentForwarding no
            //              AllowTcpForwarding no
            //              X11Forwarding no

            //Cli.Wrap("/bin/bash")
            //    .WithArguments($"useradd -p $(openssl passwd -1 {shellPassword}) {user.Username}");
            //.ExecuteAsync();
            //
            Process.Start("/bin/bash", $"-c 'useradd -p $(openssl passwd -1 {shellPassword}) {user.Username}'");
            //var ps = new ProcessStartInfo
            //{
            //    FileName = "/bin/bash",
            //    Arguments = $"-c 'useradd -p $(openssl passwd -1 {shellPassword}) {user.Username}'",
            //    RedirectStandardOutput = true
            //};

            //await Cli.Wrap("/bin/bash")
            //    .WithArguments($"-c 'useradd -p $(openssl passwd -1 {shellPassword}) {user.Username}'")
            //    .ExecuteAsync();
            //await Cli.Wrap("/bin/bash")
            //    .WithArguments($"-c 'usermod -G sftp_users {user.Username}'")
            //    .ExecuteAsync();
            //Directory.CreateDirectory($"/home/{user.Username}'");
            //await Cli.Wrap("/bin/bash")
            //    .WithArguments($"-c 'chown root /home/{user.Username}'")
            //    .ExecuteAsync();
            //await Cli.Wrap("/bin/bash")
            //    .WithArguments($"-c 'chmod 755 /home/{user.Username}'")
            //    .ExecuteAsync();
            //Directory.CreateDirectory($"/home/{user.Username}/files");
            //await Cli.Wrap("/bin/bash")
            //    .WithArguments($"-c 'chown {user.Username} /home/{user.Username}/files'")
            //    .ExecuteAsync();
            //await Cli.Wrap("/bin/bash")
            //    .WithArguments($"-c 'chmod 755 /home/{user.Username}/files'")
            //    .ExecuteAsync();

        }

        public async void Update(User user, string shellPassword)
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
                Command.ExecuteNonQuery();
            }

            await Cli.Wrap("/bin/bash")
                .WithArguments($"echo \"{shellPassword}\" | passwd --stdin {user.Username}")
                .ExecuteAsync();
        }

        public async void Delete(User user)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("DELETE FROM `users` WHERE `users`.`ID` = @id");
                Command.Parameters.AddWithValue("@id", user.ID);
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }


            await Cli.Wrap("/bin/bash")
                .WithArguments($"userdel {user.Username} --force")
                .ExecuteAsync();
        }
    }
}
