using CliWrap;
using EnvironmentServer.DAL.Enums;
using EnvironmentServer.DAL.Models;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Repositories
{
    public class EnvironmentRepository
    {
        private readonly Database DB;
        private const string ApacheConf = @"
<VirtualHost *:80>
	<FilesMatch \.php>
        SetHandler ""proxy:unix:/var/run/php/{0}.sock|fcgi://localhost/"" 
    </FilesMatch>

	ServerAdmin {1}
    ServerName {2}.{3}
	DocumentRoot {4}
    <Directory {4}>
        Options Indexes FollowSymLinks MultiViews
        AllowOverride All
        Require all granted
    </Directory>

    ErrorLog {5}/error.log
    CustomLog {5}/access.log combined
</VirtualHost>";

        public EnvironmentRepository(Database db)
        {
            DB = db;
        }

        public Environment Get(long id)
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

        public IEnumerable<Environment> GetForUser(long userID)
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

        public async Task InsertAsync(Environment environment, User user, string domain)
        {
            var dbString = user.Username + "_" + Regex.Replace(environment.Name, @"^[a-z_]([a-z0-9_-]{0,31}|[a-z0-9_-]{0,30}\$)$", "");
            //Create database for environment
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("INSERT INTO `environments` (`ID`, `users_ID_fk`, `Name`, `Address`, `Version`) VALUES "
                    + "(NULL, @userID, @name, @address, @version);");
                Command.Parameters.AddWithValue("@userID", environment.UserID);
                Command.Parameters.AddWithValue("@name", environment.Name);
                Command.Parameters.AddWithValue("@address", environment.Address);
                Command.Parameters.AddWithValue("@version", environment.Version);
                Command.Connection = connection;
                connection.Open();
                Command.ExecuteNonQuery();

                Command = new MySqlCommand("create database @database;");
                Command.Parameters.AddWithValue("@database", dbString);
                Command.Connection = connection;
                connection.Open();
                Command.ExecuteNonQuery();

                Command = new MySqlCommand("grant all on @database.* to @user@'localhost';");
                Command.Parameters.AddWithValue("@user", user.Username);
                Command.Parameters.AddWithValue("@database", dbString);
                Command.Connection = connection;
                connection.Open();
                Command.ExecuteNonQuery();
            }

            //Create environment dir            
            Directory.CreateDirectory($"/home/{user.Username}/files/{environment.Name}");
            await Cli.Wrap("/bin/bash")
                .WithArguments($"sudo chown {user.Username} /home/{user.Username}/files/{environment.Name}")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments($"sudo chmod 755 /home/{user.Username}/files/{environment.Name}")
                .ExecuteAsync();
            //Create log dir
            Directory.CreateDirectory($"/home/{user.Username}/files/logs/{environment.Name}");
            await Cli.Wrap("/bin/bash")
                .WithArguments($"sudo chown {user.Username} /home/{user.Username}/files/logs/{environment.Name}")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments($"sudo chmod 755 /home/{user.Username}/files/logs/{environment.Name}")
                .ExecuteAsync();

            //Create Apache2 configuration
            var docRoot = $"/home/{user.Username}/files/{environment.Name}";
            var logRoot = $"/home/{user.Username}/files/logs/{environment.Name}";
            var conf = System.String.Format(ApacheConf, environment.Version.AsString(), user.Email,
                environment.Name + "." + user.Username, domain, docRoot, logRoot);
            File.WriteAllText($"/etc/apache2/sites-available/{user.Username}_{environment.Name}.conf", conf);
            await Cli.Wrap("/bin/bash")
                .WithArguments($"a2ensite {user.Username}_{environment.Name}.conf")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("service apache2 reload")
                .ExecuteAsync();
        }

        public async Task DeleteAsync(Environment environment, User user, string domain)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("DELETE FROM `environments` WHERE `environments`.`ID` = @id");
                Command.Parameters.AddWithValue("@id", environment.ID);
                Command.Connection = connection;
                connection.Open();
                Command.ExecuteNonQuery();
            }

            Directory.Delete($"/home/{user.Username}/files/{environment.Name}", true);
            Directory.Delete($"/home/{user.Username}/files/logs/{environment.Name}", true);
            await Cli.Wrap("/bin/bash")
                .WithArguments($"a2dissite {user.Username}_{environment.Name}.conf")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("service apache2 reload")
                .ExecuteAsync();
            File.Delete($"/etc/apache2/sites-available/{user.Username}_{environment.Name}.conf");
        }

    }
}
