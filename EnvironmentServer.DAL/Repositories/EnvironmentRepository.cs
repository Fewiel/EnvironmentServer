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
                Address = reader.GetString(3),
                Version = (PhpVersion)reader.GetInt32(4),
                Settings = new List<EnvironmentSettingValue>(GetSettingValues(reader.GetInt64(0)))
            };
        }

        public IEnumerable<EnvironmentSettingValue> GetSettingValues(long id)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("SELECT * FROM environments_settings_values " +
                    "LEFT JOIN environments_settings " +
                    "ON environments_settings.ID = environments_settings_values.environments_settings_ID_fk " +
                    "WHERE environments_ID_fk = @id;");
                Command.Parameters.AddWithValue("@id", id);
                Command.Connection = connection;
                MySqlDataReader reader = Command.ExecuteReader();

                while (reader.Read())
                {
                    yield return new EnvironmentSettingValue
                    {
                        EnvironmentID = reader.GetInt64(0),
                        EnvironmentSettingID = reader.GetInt64(1),
                        Value = reader.GetString(2),
                        EnvironmentSetting = new EnvironmentSetting
                        {
                            ID = reader.GetInt64(3),
                            Property = reader.GetString(4)
                        }
                    };
                }
                reader.Close();
            }
        }

        public async Task<long> InsertAsync(Environment environment, User user)
        {
            var dbString = user.Username + "_" + environment.Name;
            long lastID;
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
                Command.ExecuteNonQuery();
                lastID = Command.LastInsertedId;

                Command = new MySqlCommand("create database " + dbString + ";");
                Command.Connection = connection;
                Command.ExecuteNonQuery();

                Command = new MySqlCommand("grant all on @database.* to '" + user.Username + "'@'localhost';");
                Command.Parameters.AddWithValue("@database", dbString);
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }

            //Create environment dir            
            Directory.CreateDirectory($"/home/{user.Username}/files/{environment.Name}");
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"chown {user.Username} /home/{user.Username}/files/{environment.Name}\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"chmod 755 /home/{user.Username}/files/{environment.Name}\"")
                .ExecuteAsync();
            //Create log dir
            Directory.CreateDirectory($"/home/{user.Username}/files/logs/{environment.Name}");
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"chown {user.Username} /home/{user.Username}/files/logs/{environment.Name}\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"chmod 755 /home/{user.Username}/files/logs/{environment.Name}\"")
                .ExecuteAsync();

            //Create Apache2 configuration
            var docRoot = $"/home/{user.Username}/files/{environment.Name}";
            var logRoot = $"/home/{user.Username}/files/logs/{environment.Name}";
            var conf = System.String.Format(ApacheConf, environment.Version.AsString(), user.Email,
                environment.Name + "." + user.Username, environment.Address, docRoot, logRoot);
            File.WriteAllText($"/etc/apache2/sites-available/{user.Username}_{environment.Name}.conf", conf);
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"a2ensite {user.Username}_{environment.Name}.conf\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service apache2 reload\"")
                .ExecuteAsync();

            return lastID;

            //Docker anlegen Elasticsearch 

            //        version: "3"

            //services:

            //        elasticsearch:
            //        image: elasticsearch: 6.8.1
            //      container_name: elasticsearch681
            //      hostname: EnvName - elastic
            //      networks:
            //            -web
            //      environment:
            //            -"EA_JAVA_OPTS=-Xms512m -Xms512m"
            //            - discovery.type = single - node

            //networks:
            //        web:
            //        external: false

            //#docker run  
            //#cron zum abschalten nachts
            //#möglich persistent
        }

        public async Task DeleteAsync(Environment environment, User user, string domain)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("DELETE FROM `environments` WHERE `environments`.`ID` = @id");
                Command.Parameters.AddWithValue("@id", environment.ID);
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }

            Directory.Delete($"/home/{user.Username}/files/{environment.Name}", true);
            Directory.Delete($"/home/{user.Username}/files/logs/{environment.Name}", true);

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"a2dissite {user.Username}_{environment.Name}.conf\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service apache2 reload\"")
                .ExecuteAsync();

            File.Delete($"/etc/apache2/sites-available/{user.Username}_{environment.Name}.conf");
        }

    }
}
