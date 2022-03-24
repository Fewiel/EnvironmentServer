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
pm.max_children = 20
pm.start_servers = 2
pm.min_spare_servers = 1
pm.max_spare_servers = 3

php_admin_value[open_basedir] = /home/{0}/files:/dev/urandom
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
            using var connection = DB.GetConnection();
            foreach (var usr in connection.Query<User>("select * from users;"))
            {
                usr.UserInformation = DB.UserInformation.Get(usr.ID);
                yield return usr;
            }
        }

        public User GetByID(long ID)
        {
            using var connection = DB.GetConnection();
            var usr = connection.QuerySingleOrDefault<User>("select * from users where ID = @id;", new
            {
                id = ID
            });
            usr.UserInformation = DB.UserInformation.Get(usr.ID);
            return usr;
        }

        public User GetByUsername(string username)
        {
            using var connection = DB.GetConnection();
            var usr = connection.QuerySingleOrDefault<User>("select * from users where Username = @username AND `active` = 1;", new
            {
                username
            });

            if (usr != null)
                usr.UserInformation = DB.UserInformation.Get(usr.ID);

            return usr;
        }

        public User GetByMail(string mail)
        {
            using var connection = DB.GetConnection();
            var usr = connection.QuerySingleOrDefault<User>("select * from users where Email = @mail AND `active` = 1;", new
            {
                mail
            });

            if (usr != null)
                usr.UserInformation = DB.UserInformation.Get(usr.ID);

            return usr;
        }

        public async Task InsertAsync(User user, string shellPassword)
        {
            DB.Logs.Add("DAL", "Insert user " + user.Username);
            using var connection = DB.GetConnection();

            connection.Execute("INSERT INTO `users` (`ID`, `Email`, `Username`, `Password`, `IsAdmin`, `ExpirationDate`) "
                     + "VALUES (NULL, @email, @username, @password, @isAdmin, @exp)", new
                     {
                         email = user.Email,
                         username = user.Username,
                         password = user.Password,
                         isAdmin = user.IsAdmin,
                         exp = user.ExpirationDate
                     });

            connection.Execute($"create user {MySqlHelper.EscapeString(user.Username)}@'localhost' identified by @password;", new
            {
                password = shellPassword
            });

            DB.Logs.Add("DAL", "Start Useradd: " + user.Username);

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"useradd -p $(openssl passwd -1 $'{shellPassword}') {user.Username}\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"usermod -G sftp_users {user.Username}\"")
                .ExecuteAsync();

            DB.Logs.Add("DAL", "Create user homefolder: " + user.Username);
            Directory.CreateDirectory($"/home/{user.Username}");

            await Cli.Wrap("/bin/bash")
               .WithArguments($"-c \"chown {user.Username}:sftp_users /home/{user.Username}\"")
               .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"chmod 700 /home/{user.Username}\"")
                .ExecuteAsync();

            DB.Logs.Add("DAL", "Create user files folder: " + user.Username);
            Directory.CreateDirectory($"/home/{user.Username}/files");
            Directory.CreateDirectory($"/home/{user.Username}/files/php");
            Directory.CreateDirectory($"/home/{user.Username}/files/php/tmp");
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"chown -R {user.Username}:sftp_users /home/{user.Username}/files\"")
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

            connection.Execute("UPDATE mysql.user SET Super_Priv='Y';");

            connection.Execute("FLUSH PRIVILEGES;");

            DB.Logs.Add("DAL", "New User added: " + user.Username);
        }

        public static async Task UpdateChrootForUserAsync(string user)
        {
            var path = "/home/" + user;

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"chown {user}:sftp_users {path}\"")
                .ExecuteAsync();

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"chmod 700 /home/{user}\"")
                .ExecuteAsync();
        }

        public async Task RegenerateConfig()
        {
            foreach (var user in DB.Users.GetUsers())
            {
                try
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
                catch (Exception ex)
                {
                    DB.Logs.Add("DAL", ex.ToString());
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
            using var connection = DB.GetConnection();

            connection.Execute("UPDATE `users` SET "
                + "`Email` = @email, `Username` = @username, `Password` = @password," +
                " `IsAdmin` = @isAdmin WHERE `users`.`ID` = @id", new
                {
                    id = user.ID,
                    email = user.Email,
                    username = user.Username,
                    password = user.Password,
                    isAdmin = user.IsAdmin
                });

            connection.Execute($"ALTER USER {MySqlHelper.EscapeString(user.Username)}@'localhost' IDENTIFIED BY @password;", new
            {
                password = shellPassword
            });

            connection.Execute("UPDATE mysql.user SET Super_Priv='Y' WHERE user=@user;",
                new
                {
                    user = user.Username
                });

            connection.Execute("FLUSH PRIVILEGES;");

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"echo -e '{user.Username}:{shellPassword}' | chpasswd\"")
                .ExecuteAsync();
        }

        public async Task UpdateByAdminAsync(User usr, bool newPassword)
        {
            DB.Logs.Add("DAL", "Admin Update user " + usr.Username);

            var user = new User();
            using var connection = DB.GetConnection();
            UpdateLastUse(usr);

            if (newPassword)
            {
                var shellPassword = RandomPasswordString(32);

                user = new User
                {
                    ID = usr.ID,
                    Username = usr.Username,
                    Email = usr.Email,
                    IsAdmin = usr.IsAdmin,
                    Password = PasswordHasher.Hash(shellPassword),
                    Active = user.Active,
                    LastUsed = user.LastUsed,
                    ExpirationDate = user.ExpirationDate
                };

                connection.Execute($"ALTER USER {MySqlHelper.EscapeString(user.Username)}@'localhost' IDENTIFIED BY @password;", new
                {
                    user = user.Username + "@localhost",
                    password = shellPassword
                });

                await Cli.Wrap("/bin/bash")
                    .WithArguments($"-c \"echo -e '{user.Username}:{shellPassword}' | chpasswd\"")
                    .ExecuteAsync();

                DB.Mail.Send("Password reseted", string.Format(DB.Settings.Get("mail_account_password").Value, usr.Username, shellPassword), usr.Email);
            }
            else
            {
                user = usr;
            }

            connection.Execute("UPDATE `users` SET "
                + "`Email` = @email, `Username` = @username, `Password` = @password," +
                " `IsAdmin` = @isAdmin, `Active` = @active, `LastUsed` = @lastused, `ExpirationDate` = @exp WHERE `users`.`ID` = @id", new
                {
                    id = user.ID,
                    email = user.Email,
                    username = user.Username,
                    password = user.Password,
                    isAdmin = user.IsAdmin,
                    active = user.Active,
                    lastused =  user.LastUsed,
                    exp = user.ExpirationDate
                });
        }

        public async Task LockUserAsync(User usr)
        {
            DB.Logs.Add("DAL", "Lock user " + usr.Username);

            var user = usr;

            var shellPassword = RandomPasswordString(32);

            user.Password = shellPassword;

            using var connection = DB.GetConnection();

            connection.Execute($"ALTER USER {MySqlHelper.EscapeString(user.Username)}@'localhost' IDENTIFIED BY @password;", new
            {
                user = user.Username + "@'localhost'",
                password = shellPassword
            });

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"echo -e '{user.Username}:{shellPassword}' | chpasswd\"")
                .ExecuteAsync();

            connection.Execute("UPDATE `users` SET "
                + "`Email` = @email, `Username` = @username, `Password` = @password," +
                " `IsAdmin` = @isAdmin, `Active` = @active, `LastUsed` = @lastused, `ExpirationDate` = @exp WHERE `users`.`ID` = @id", new
                {
                    id = user.ID,
                    email = user.Email,
                    username = user.Username,
                    password = user.Password,
                    isAdmin = user.IsAdmin,
                    active = user.Active,
                    lastused = user.LastUsed,
                    exp = user.ExpirationDate
                });
        }

        public void ChangeActiveState(User user, bool active)
        {
            using var connection = DB.GetConnection();
            connection.Execute("UPDATE `users` SET "
                 + "`Active` = @active WHERE `users`.`ID` = @id", new
                 {
                     id = user.ID,
                     active
                 });
        }

        public IEnumerable<User>GetTempUsers()
        {
            using var connection = DB.GetConnection();
            return connection.Query<User>("SELECT * FROM `users` Where ExpirationDate IS NOT NULL;");
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
            using var connection = DB.GetConnection();
            connection.Execute("DELETE FROM `users` WHERE `users`.`ID` = @id", new
            {
                id = user.ID
            });

            connection.Execute($"DROP USER {MySqlHelper.EscapeString(user.Username)}@'localhost';");

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"userdel {user.Username} --force\"")
                .ExecuteAsync();

            Directory.Delete($"/home/{user.Username}", true);
            DB.Logs.Add("DAL", "Delete user complete for " + user.Username);
        }

        public void UpdateSSHKey(string key, long userid)
        {
            using var connection = DB.GetConnection();
            connection.Execute("UPDATE `users` SET "
                     + "`SSHPublicKey` = @key where `ID` = @id", new
                     {
                         id = userid,
                         key
                     });
        }

        public void UpdateLastUse(User user)
        {
            using var connection = DB.GetConnection();
            connection.Execute("UPDATE `users` SET "
                     + "`LastUsed` = @used where `ID` = @id", new
                     {
                         id = user.ID,
                         used = DateTime.Now
                     });
        }

        public async Task SetSSHKeyAsync(User user)
        {
            var usr = DB.Users.GetByID(user.ID);

            Directory.CreateDirectory($"/home/{usr.Username}/.ssh");
            File.WriteAllText($"/home/{usr.Username}/.ssh/authorized_keys", usr.SSHPublicKey);
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"chown -R {usr.Username}:sftp_users /home/{usr.Username}/.ssh\"")
                .ExecuteAsync();
        }

        public void SendSSHConfirmation(User user)
        {
            var token = DB.Tokens.Generate(user.ID);
            DB.Mail.Send("Shopware Environment Server Account",
                string.Format(DB.Settings.Get("mail_account_sshkey").Value, user.Username, token.ToString()), user.Email);
        }

        public void ForgotPassword(string mail)
        {
            if (string.IsNullOrEmpty(mail))
                return;

            var usr = GetByMail(mail);

            if (usr == null)
                return;

            var token = DB.Tokens.Generate(usr.ID);
            DB.Mail.Send("Shopware Environment Server Account",
                string.Format(DB.Settings.Get("mail_account_password_recovery").Value, usr.Username, token.ToString(), usr.Email), usr.Email);
        }

        public async Task<bool> ResetPasswordAsync(string token, string mail, string password)
        {
            var usr = GetByMail(mail);

            if (usr == null || string.IsNullOrEmpty(token) || !Guid.TryParse(token, out Guid guid))
                return false;

            if (!DB.Tokens.Use(guid, usr.ID))
                return false;

            var update_usr = new User
            {
                ID = usr.ID,
                Username = usr.Username,
                Email = usr.Email,
                Password = PasswordHasher.Hash(password),
                IsAdmin = usr.IsAdmin
            };

            await UpdateAsync(update_usr, password);
            return true;
        }
    }
}
