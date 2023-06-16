using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using EnvironmentServer.Util;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Repositories
{
    public class UsersRepository
    {
        private readonly Database DB;
        private string phpfpm;

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
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            foreach (var usr in c.Connection.Query<User>("select * from users;"))
            {
                usr.UserInformation = DB.UserInformation.Get(usr.ID);
                yield return usr;
            }
        }

        public User GetByID(long ID)
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var usr = c.Connection.QuerySingleOrDefault<User>("select * from users where ID = @id;", new
            {
                id = ID
            });
            usr.UserInformation = DB.UserInformation.Get(usr.ID);
            return usr;
        }

        public User GetByUsername(string username)
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var usr = c.Connection.QuerySingleOrDefault<User>("select * from users where Username = @username", new
            {
                username
            });

            if (usr != null)
                usr.UserInformation = DB.UserInformation.Get(usr.ID);

            return usr;
        }

        public User GetByMail(string mail)
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            var usr = c.Connection.QuerySingleOrDefault<User>("select * from users where Email = @mail AND `active` = 1;", new
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
            using var c = new MySQLConnectionWrapper(DB.ConnString);

            c.Connection.Execute("INSERT INTO `users` (`ID`, `Email`, `Username`, `Password`, `IsAdmin`, `ExpirationDate`, `RoleID`, `ForcePasswordReset`) "
                     + "VALUES (NULL, @email, @username, @password, @isAdmin, @exp, @rid, @pwreset)", new
                     {
                         email = user.Email,
                         username = user.Username,
                         password = user.Password,
                         isAdmin = user.IsAdmin,
                         exp = user.ExpirationDate,
                         rid = user.RoleID, 
                         pwreset = true
                     });

            c.Connection.Execute($"create user {MySqlHelper.EscapeString(user.Username)}@'localhost' identified by @password;", new
            {
                password = shellPassword
            });

            DB.Logs.Add("DAL", "Start Useradd: " + user.Username);

            await Bash.UserAddAsync(user.Username, shellPassword);
            await Bash.UserModGroupAsync(user.Username, "sftp_users");
            await Bash.UserModGroupAsync(user.Username, "www-data", true);

            DB.Logs.Add("DAL", "Create user homefolder: " + user.Username);
            Directory.CreateDirectory($"/home/{user.Username}");

            await Bash.ChmodAsync("700", $"/home/{user.Username}");
            await Bash.ChownAsync(user.Username, "sftp_users", $"/home/{user.Username}");

            DB.Logs.Add("DAL", "Create user files folder: " + user.Username);
            Directory.CreateDirectory($"/home/{user.Username}/files");
            Directory.CreateDirectory($"/home/{user.Username}/files/php");
            Directory.CreateDirectory($"/home/{user.Username}/files/php/tmp");

            await Bash.ChownAsync(user.Username, "sftp_users", $"/home/{user.Username}/files", true);
            await Bash.ChmodAsync("755", $"/home/{user.Username}/files");

            DB.Logs.Add("DAL", "Create user php-fpm - " + user.Username);

            phpfpm = DB.Settings.Get("phpfpm").Value;

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
            conf = string.Format(phpfpm, user.Username, "php8.2-fpm");
            File.WriteAllText($"/etc/php/8.2/fpm/pool.d/{user.Username}.conf", conf);

            await Bash.ServiceReloadAsync("php5.6-fpm");
            await Bash.ServiceReloadAsync("php7.2-fpm");
            await Bash.ServiceReloadAsync("php7.4-fpm");
            await Bash.ServiceReloadAsync("php8.0-fpm");
            await Bash.ServiceReloadAsync("php8.1-fpm");
            await Bash.ServiceReloadAsync("php8.2-fpm");

            File.Create($"/home/{user.Username}/files/php/php-error.log");

            DB.Mail.Send("Shopware Environment Server Account",
                string.Format(DB.Settings.Get("mail_account_created").Value, user.Username, shellPassword), user.Email);

            c.Connection.Execute("UPDATE mysql.user SET Super_Priv='Y';");

            c.Connection.Execute("FLUSH PRIVILEGES;");

            DB.Logs.Add("DAL", "New User added: " + user.Username);
        }

        public static async Task UpdateChrootForUserAsync(string user)
        {
            var path = "/home/" + user;

            await Bash.ChownAsync(user, "sftp_users", path);
            await Bash.ChmodAsync("700", path);
        }

        public async Task RegenerateConfig(bool includePhp = false)
        {
            phpfpm = DB.Settings.Get("phpfpm").Value;

            foreach (var user in DB.Users.GetUsers())
            {
                if (!File.Exists($"/home/{user.Username}/files/php/php-error.log"))
                    File.Create($"/home/{user.Username}/files/php/php-error.log");
                await Bash.ChownAsync(user.Username, "sftp_users", $"/home/{user.Username}/files/php/php-error.log");
                try
                {
                    DB.Logs.Add("RegenerateConfig", "Regenerate Config for " + user.Username);
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
                    conf = string.Format(phpfpm, user.Username, "php8.2-fpm");
                    File.WriteAllText($"/etc/php/8.2/fpm/pool.d/{user.Username}.conf", conf);
                    if (includePhp)
                    {
                        foreach (var env in DB.Environments.GetForUser(user.ID))
                        {
                            await DB.Environments.UpdatePhpAsync(env.ID, user, env.Version);
                        }
                    }
                }
                catch (Exception ex)
                {
                    DB.Logs.Add("DAL", ex.ToString());
                }
            }

            await Bash.ServiceReloadAsync("php5.6-fpm");
            await Bash.ServiceReloadAsync("php7.2-fpm");
            await Bash.ServiceReloadAsync("php7.4-fpm");
            await Bash.ServiceReloadAsync("php8.0-fpm");
            await Bash.ServiceReloadAsync("php8.1-fpm");
            await Bash.ServiceReloadAsync("php8.2-fpm");
            await Bash.ReloadApacheAsync();
        }

        public async Task UpdateAsync(User user, string shellPassword)
        {
            DB.Logs.Add("DAL", "Update user " + user.Username);
            using var c = new MySQLConnectionWrapper(DB.ConnString);

            c.Connection.Execute("UPDATE `users` SET "
                + "`Email` = @email, `Username` = @username, `Password` = @password," +
                " `IsAdmin` = @isAdmin, `RoleID` = @rid, `ForcePasswordReset` = @pwreset WHERE `users`.`ID` = @id", new
                {
                    id = user.ID,
                    email = user.Email,
                    username = user.Username,
                    password = user.Password,
                    isAdmin = user.IsAdmin,
                    rid = user.RoleID,
                    pwreset = user.ForcePasswordReset
                });

            c.Connection.Execute($"ALTER USER {MySqlHelper.EscapeString(user.Username)}@'localhost' IDENTIFIED BY @password;", new
            {
                password = shellPassword
            });

            c.Connection.Execute("UPDATE mysql.user SET Super_Priv='Y' WHERE user=@user;",
                new
                {
                    user = user.Username
                });

            c.Connection.Execute("FLUSH PRIVILEGES;");

            await Bash.CommandAsync($"echo -e '{user.Username}:{shellPassword}' | chpasswd", log: false);
        }

        public async Task UpdateByAdminAsync(User usr, bool newPassword)
        {
            DB.Logs.Add("DAL", "Admin Update user " + usr.Username);

            var user = new User();
            using var c = new MySQLConnectionWrapper(DB.ConnString);
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
                    Active = usr.Active,
                    LastUsed = usr.LastUsed,
                    ExpirationDate = usr.ExpirationDate,
                    RoleID = usr.RoleID
                };

                c.Connection.Execute($"ALTER USER {MySqlHelper.EscapeString(user.Username)}@'localhost' IDENTIFIED BY @password;", new
                {
                    user = user.Username + "@localhost",
                    password = shellPassword
                });

                await Bash.CommandAsync($"echo -e '{user.Username}:{shellPassword}' | chpasswd", log: false);

                DB.Mail.Send("Password reseted", string.Format(DB.Settings.Get("mail_account_password").Value, usr.Username, shellPassword), usr.Email);
            }
            else
            {
                user = usr;
            }

            c.Connection.Execute("UPDATE `users` SET "
                + "`Email` = @email, `Username` = @username, `Password` = @password," +
                " `IsAdmin` = @isAdmin, `Active` = @active, `LastUsed` = @lastused, `ExpirationDate` = @exp, `RoleID` = @rid, `ForcePasswordReset` = @pwreset WHERE `users`.`ID` = @id", new
                {
                    id = user.ID,
                    email = user.Email,
                    username = user.Username,
                    password = user.Password,
                    isAdmin = user.IsAdmin,
                    active = user.Active,
                    lastused = user.LastUsed,
                    exp = user.ExpirationDate,
                    rid = user.RoleID, 
                    pwreset = user.ForcePasswordReset
                });
        }

        public async Task LockUserAsync(User usr)
        {
            DB.Logs.Add("DAL", "Lock user " + usr.Username);

            var user = usr;

            var shellPassword = RandomPasswordString(32);

            user.Password = shellPassword;

            using var c = new MySQLConnectionWrapper(DB.ConnString);

            c.Connection.Execute($"ALTER USER {MySqlHelper.EscapeString(user.Username)}@'localhost' IDENTIFIED BY @password;", new
            {
                user = user.Username + "@'localhost'",
                password = shellPassword
            });

            await Bash.CommandAsync($"echo -e '{user.Username}:{shellPassword}' | chpasswd", log: false);

            c.Connection.Execute("UPDATE `users` SET "
                + "`Email` = @email, `Username` = @username, `Password` = @password," +
                " `IsAdmin` = @isAdmin, `Active` = @active, `LastUsed` = @lastused, `ExpirationDate` = @exp, `RoleID` = @rid, `ForcePasswordReset` = @pwreset WHERE `users`.`ID` = @id", new
                {
                    id = user.ID,
                    email = user.Email,
                    username = user.Username,
                    password = user.Password,
                    isAdmin = user.IsAdmin,
                    active = user.Active,
                    lastused = user.LastUsed,
                    exp = user.ExpirationDate,
                    rid = user.RoleID, 
                    pwreset = user.ForcePasswordReset
                });
        }

        public void ChangeActiveState(User user, bool active)
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            c.Connection.Execute("UPDATE `users` SET "
                 + "`Active` = @active WHERE `users`.`ID` = @id", new
                 {
                     id = user.ID,
                     active
                 });
        }

        public IEnumerable<User> GetTempUsers()
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            return c.Connection.Query<User>("SELECT * FROM `users` Where ExpirationDate IS NOT NULL;");
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
            File.Delete($"/etc/php/8.2/fpm/pool.d/{user.Username}.conf");

            await Bash.ServiceReloadAsync("php5.6-fpm");
            await Bash.ServiceReloadAsync("php7.2-fpm");
            await Bash.ServiceReloadAsync("php7.4-fpm");
            await Bash.ServiceReloadAsync("php8.0-fpm");
            await Bash.ServiceReloadAsync("php8.1-fpm");
            await Bash.ServiceReloadAsync("php8.2-fpm");

            DB.Logs.Add("DAL", "Delete user " + user.Username);
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            c.Connection.Execute("DELETE FROM `users` WHERE `users`.`ID` = @id", new
            {
                id = user.ID
            });

            c.Connection.Execute($"DROP USER {MySqlHelper.EscapeString(user.Username)}@'localhost';");

            await Bash.CommandAsync($"userdel {user.Username} --force");

            Directory.Delete($"/home/{user.Username}", true);
            DB.Logs.Add("DAL", "Delete user complete for " + user.Username);
        }

        public void UpdateSSHKey(string key, long userid)
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            c.Connection.Execute("UPDATE `users` SET "
                     + "`SSHPublicKey` = @key where `ID` = @id", new
                     {
                         id = userid,
                         key
                     });
        }

        public void UpdateLastUse(User user)
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            c.Connection.Execute("UPDATE `users` SET "
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

            await Bash.ChownAsync(usr.Username, "sftp_users", $"/home/{usr.Username}/.ssh", true);
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
                IsAdmin = usr.IsAdmin,
                RoleID = usr.RoleID,
                ForcePasswordReset = false
            };

            await UpdateAsync(update_usr, password);
            return true;
        }
    }
}
