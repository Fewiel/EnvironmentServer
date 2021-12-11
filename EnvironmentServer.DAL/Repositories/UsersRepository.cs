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

        private const string chroot = @"#!/bin/bash
# This script can be used to create simple chroot environment
# Written by LinuxCareer.com <http://linuxcareer.com/>
# (c) 2013 LinuxCareer under GNU GPL v3.0+

#!/bin/bash

CHROOT={0}
USER={1}
if [ -f $CHROOT]; then
mkdir $CHROOT
fi

for i in $(ldd $* | grep -v dynamic | cut -d "" "" -f 3 | sed 's/://' | sort | uniq )
  do
    cp --parents $i $CHROOT
  done

# ARCH amd64
if [ -f /lib64/ld-linux-x86-64.so.2 ]; then
cp --parents /lib64/ld-linux-x86-64.so.2 /$CHROOT
fi

# ARCH i386
if [ -f  /lib/ld-linux.so.2 ]; then
cp --parents /lib/ld-linux.so.2 /$CHROOT
fi

mkdir $CHROOT/lib/terminfo
mkdir $CHROOT/lib/terminfo/x
cp /lib/terminfo/x/xterm $CHROOT/lib/terminfo/x/

chown -R {1}:sftp_users {0}/*
chown {1}:root {0}/files

chsh --shell /bin/bash {1}";

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
                connection.Execute("INSERT INTO `users` (`ID`, `Email`, `Username`, `Password`, `IsAdmin`) "
                     + "VALUES (NULL, @email, @username, @password, @isAdmin)", new
                     {
                         email = user.Email,
                         username = user.Username,
                         password = user.Password,
                         isAdmin = user.IsAdmin
                     });

                connection.Execute($"create user {MySqlHelper.EscapeString(user.Username)}@'localhost' identified by @password;", new
                {
                    password = shellPassword
                });

            }

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

            using (var connection = DB.GetConnection())
            {
                connection.Execute("UPDATE mysql.user SET Super_Priv='Y';");
            }
            using (var connection = DB.GetConnection())
            {
                connection.Execute("FLUSH PRIVILEGES;");
            }

            await SetupChrootForUserAsync(user.Username);
            DB.Logs.Add("DAL", "New User added: " + user.Username);
        }

        private async Task SetupChrootForUserAsync(string user)
        {
            var path = "/home/" + user;
            var shell = string.Format(chroot, path, user);

            File.WriteAllText("/tmp/chroot_" + user + ".sh", shell);

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"bash /tmp/chroot_" + user + ".sh /bin/{ls,cat,echo,rm,bash,sh} /usr/sbin/{phpenmod,phpdismod} /usr/bin/{php*,unzip,nano,vi,mkdir,zip,tar,chmod,chown,env,mysql,mysqldump,git} /usr/share/zoneinfo /etc/hosts\"")
                .ExecuteAsync();
        }

        public async Task UpdateChrootForUserAsync(string user)
        {
            var path = "/home/" + user;
            var shell = string.Format(chroot, path, user);

            if (Directory.Exists(path + "/lib"))
                Directory.Delete(path + "/lib", true);

            if (Directory.Exists(path + "/lib64"))
                Directory.Delete(path + "/lib64", true);

            if (Directory.Exists(path + "/usr"))
                Directory.Delete(path + "/usr", true);

            if (Directory.Exists(path + "/etc"))
                Directory.Delete(path + "/etc", true);

            if (Directory.Exists(path + "/bin"))
                Directory.Delete(path + "/bin", true);

            if (!Directory.Exists(path + "/home"))
                Directory.CreateDirectory(path + "/home");

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"chown {user}:{user} {path}/home\"")
                .ExecuteAsync();

            File.WriteAllText("/tmp/chroot_" + user + ".sh", shell);

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"bash /tmp/chroot_" + user + ".sh /bin/{ls,cat,echo,rm,bash,sh} /usr/sbin/{phpenmod,phpdismod} /usr/bin/{php*,unzip,nano,vi,mkdir,zip,tar,chmod,chown,env,mysql,mysqldump,git} /usr/share/zoneinfo /etc/hosts\"")
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
            using (var connection = DB.GetConnection())
            {
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
            }

            using (var connection = DB.GetConnection())
            {
                connection.Execute($"ALTER USER {MySqlHelper.EscapeString(user.Username)}@'localhost' IDENTIFIED BY @password;", new
                {
                    password = shellPassword
                });
            }


            

            using (var connection = DB.GetConnection())
            {
                connection.Execute("UPDATE mysql.user SET Super_Priv='Y' WHERE user=@user;",
                    new
                    {
                        user = user.Username
                    });
            }

            using (var connection = DB.GetConnection())
            {
                connection.Execute("FLUSH PRIVILEGES;");
            }

            await Cli.Wrap("/bin/bash")
            .WithArguments($"-c \"echo -e '{user.Username}:{shellPassword}' | chpasswd\"")
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
                    connection.Execute($"ALTER USER {MySqlHelper.EscapeString(user.Username)}@'localhost' IDENTIFIED BY @password;", new
                    {
                        user = user.Username + "@localhost",
                        password = shellPassword
                    });
                }

                await Cli.Wrap("/bin/bash")
                    .WithArguments($"-c \"echo -e '{user.Username}:{shellPassword}' | chpasswd\"")
                    .ExecuteAsync();

                DB.Mail.Send("Password reseted", string.Format(DB.Settings.Get("mail_account_password").Value, usr.Username, shellPassword), usr.Email);
            }
            else
            {
                user = usr;
            }

            using (var connection = DB.GetConnection())
            {
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
                connection.Execute($"ALTER USER {MySqlHelper.EscapeString(user.Username)}@'localhost' IDENTIFIED BY @password;", new
                {
                    user = user.Username + "@'localhost'",
                    password = shellPassword
                });
            }

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"echo -e '{user.Username}:{shellPassword}' | chpasswd\"")
                .ExecuteAsync();

            using (var connection = DB.GetConnection())
            {
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
                connection.Execute("DELETE FROM `users` WHERE `users`.`ID` = @id", new
                {
                    id = user.ID
                });
            }
            using (var connection = DB.GetConnection())
            {
                connection.Execute($"DROP USER {MySqlHelper.EscapeString(user.Username)}@'localhost';");
            }

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"userdel {user.Username} --force\"")
                .ExecuteAsync();

            Directory.Delete($"/home/{user.Username}", true);
            DB.Logs.Add("DAL", "Delete user complete for " + user.Username);
        }       
    }
}
