using Dapper;
using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Utility;
using EnvironmentServer.Util;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions;

public class ReRunUserInstallation : ActionBase
{
    public override string ActionIdentifier => "UserInstallation";

    public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
    {
        var db = sp.GetService<Database>();
        using var c = new MySQLConnectionWrapper(db.ConnString);

        foreach (var usr in db.Users.GetUsers())
        {
            if (usr.Username == "admin")
                continue;

            db.Logs.Add("DAL", "Import user: " + usr.Username);

            c.Connection.Execute($"create user {MySqlHelper.EscapeString(usr.Username)}@'localhost' identified by @password;", new
            {
                password = Guid.NewGuid().ToString()[..16],
            });
            c.Connection.Execute("UPDATE mysql.user SET Super_Priv='Y';");
            c.Connection.Execute("FLUSH PRIVILEGES;");

            await Bash.UserAddAsync(usr.Username, Guid.NewGuid().ToString());
            await Bash.UserModGroupAsync(usr.Username, "sftp_users");

            Directory.CreateDirectory($"/home/{usr.Username}");
            File.Create($"/home/{usr.Username}/files/php/php-error.log");

            await Bash.ChmodAsync("700", $"/home/{usr.Username}");
            await Bash.ChownAsync(usr.Username, "sftp_users", $"/home/{usr.Username}");

            Directory.CreateDirectory($"/home/{usr.Username}/files");
            Directory.CreateDirectory($"/home/{usr.Username}/files/php");
            Directory.CreateDirectory($"/home/{usr.Username}/files/php/tmp");

            await Bash.ChownAsync(usr.Username, "sftp_users", $"/home/{usr.Username}/files", true);
            await Bash.ChmodAsync("755", $"/home/{usr.Username}/files");

            var phpfpm = db.Settings.Get("phpfpm").Value;

            var conf = string.Format(phpfpm, usr.Username, "php5.6-fpm");
            File.WriteAllText($"/etc/php/5.6/fpm/pool.d/{usr.Username}.conf", conf);
            conf = string.Format(phpfpm, usr.Username, "php7.2-fpm");
            File.WriteAllText($"/etc/php/7.2/fpm/pool.d/{usr.Username}.conf", conf);
            conf = string.Format(phpfpm, usr.Username, "php7.4-fpm");
            File.WriteAllText($"/etc/php/7.4/fpm/pool.d/{usr.Username}.conf", conf);
            conf = string.Format(phpfpm, usr.Username, "php8.0-fpm");
            File.WriteAllText($"/etc/php/8.0/fpm/pool.d/{usr.Username}.conf", conf);
            conf = string.Format(phpfpm, usr.Username, "php8.1-fpm");
            File.WriteAllText($"/etc/php/8.1/fpm/pool.d/{usr.Username}.conf", conf);
            conf = string.Format(phpfpm, usr.Username, "php8.2-fpm");
            File.WriteAllText($"/etc/php/8.2/fpm/pool.d/{usr.Username}.conf", conf);

            await Bash.ServiceReloadAsync("php5.6-fpm");
            await Bash.ServiceReloadAsync("php7.2-fpm");
            await Bash.ServiceReloadAsync("php7.4-fpm");
            await Bash.ServiceReloadAsync("php8.0-fpm");
            await Bash.ServiceReloadAsync("php8.1-fpm");
            await Bash.ServiceReloadAsync("php8.2-fpm");
        }
    }
}