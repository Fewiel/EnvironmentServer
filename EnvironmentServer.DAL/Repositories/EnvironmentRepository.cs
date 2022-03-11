using CliWrap;
using Dapper;
using EnvironmentServer.DAL.Enums;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.StringConstructors;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Ubiety.Dns.Core.Records.NotUsed;

namespace EnvironmentServer.DAL.Repositories
{
    public class EnvironmentRepository
    {
        private readonly Database DB;

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
                var Command = new MySqlCommand("select * from environments where users_ID_fk = @id ORDER BY Sorting DESC;");
                Command.Parameters.AddWithValue("@id", userID);
                Command.Connection = connection;
                MySqlDataReader reader = Command.ExecuteReader();

                while (reader.Read())
                    yield return FromReader(reader);

                reader.Close();
            }
        }
        public IEnumerable<Environment> GetAll()
        {
            using (var connection = DB.GetConnection())
            {
                foreach (var env in connection.Query<Environment>("select * from environments;"))
                {
                    env.Settings = new List<EnvironmentSettingValue>(GetSettingValues(env.ID));
                    yield return env;
                }
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
                DBPassword = reader.GetString(5),
                Settings = new List<EnvironmentSettingValue>(GetSettingValues(reader.GetInt64(0))),
                Sorting = reader.GetInt32(6)
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

        public async Task<long> InsertAsync(Environment environment, User user, bool sw6)
        {
            DB.Logs.Add("DAL", "Insert Environment " + environment.Name + " for " + user.Username);
            var dbString = user.Username + "_" + environment.Name;
            long lastID;

            var dbPassword = UsersRepository.RandomPasswordString(16);

            //Create database for environment
            using (var connection = DB.GetConnection())
            {
                lastID = connection.QuerySingle<int>("INSERT INTO `environments` (`ID`, `users_ID_fk`, `Name`, `Address`, `Version`, `DBPassword`) " +
                    "VALUES (NULL, @userID, @envName, @envAddress, @version, @dbpassword); SELECT LAST_INSERT_ID();", new
                    {
                        userID = environment.UserID,
                        envName = environment.Name,
                        envAddress = environment.Address,
                        version = environment.Version,
                        dbpassword = dbPassword
                    });

                connection.Execute($"create database {MySqlHelper.EscapeString(dbString)};");

                connection.Execute($"create user {MySqlHelper.EscapeString(dbString)}@'localhost' identified by @password;", new
                {
                    password = dbPassword
                });

                connection.Execute($"grant all on {MySqlHelper.EscapeString(dbString)}.* to '{MySqlHelper.EscapeString(dbString)}'@'localhost';");

                connection.Execute($"grant all on {MySqlHelper.EscapeString(dbString)}.* to '{MySqlHelper.EscapeString(user.Username)}'@'localhost';");

                connection.Execute("UPDATE mysql.user SET Super_Priv='Y';");
                connection.Execute("FLUSH PRIVILEGES;");
            }

            //Create environment dir            
            Directory.CreateDirectory($"/home/{user.Username}/files/{environment.Name}");
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"chown {user.Username}:sftp_users /home/{user.Username}/files/{environment.Name}\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"chmod 755 /home/{user.Username}/files/{environment.Name}\"")
                .ExecuteAsync();
            //Create log dir
            Directory.CreateDirectory($"/home/{user.Username}/files/logs/{environment.Name}");
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"chown {user.Username}:sftp_users /home/{user.Username}/files/logs/{environment.Name}\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"chmod 755 /home/{user.Username}/files/logs/{environment.Name}\"")
                .ExecuteAsync();

            //Create Apache2 configuration            
            var docRoot = $"/home/{user.Username}/files/{environment.Name}{(sw6 ? "/public" : "")}";
            var logRoot = $"/home/{user.Username}/files/logs/{environment.Name}";

            var builder = ApacheConfConstructor.Construct
                .WithVersion(environment.Version)
                .WithEmail(user.Email)
                .WithAddress(environment.Address)
                .WithDocRoot(docRoot)
                .WithLogRoot(logRoot)
                .WithSSLCertFile(DB.Settings.Get("SSLCertificateFile").Value)
                .WithSSLKeyFile(DB.Settings.Get("SSLCertificateKeyFile").Value)
                .WithSSLChainFile(DB.Settings.Get("SSLCertificateChainFile").Value)
                .WithUsername(user.Username);

            var conf = File.Exists(".nossl") ? builder.Build() : builder.BuildSSL();

            File.WriteAllText($"/etc/apache2/sites-available/{user.Username}_{environment.Name}.conf", conf);
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service apache2 reload\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"a2ensite {user.Username}_{environment.Name}.conf\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service apache2 reload\"")
                .ExecuteAsync();

            return lastID;
        }

        public async Task UpdatePhpAsync(long id, User user, PhpVersion version)
        {
            DB.Logs.Add("DAL", $"Update PHP Version of ID {id} to {version}");
            var environment = DB.Environments.Get(id);
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("UPDATE `environments` SET `Version` = @version WHERE `environments`.`ID` = @id;");
                Command.Parameters.AddWithValue("@version", version);
                Command.Parameters.AddWithValue("@id", id);
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"a2dissite {user.Username}_{environment.Name}.conf\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service apache2 reload\"")
                .ExecuteAsync();

            var envVersion = environment.Settings.Find(s => s.EnvironmentSetting.Property == "sw_version");
            var sw_version = envVersion == null ? "N/A" : envVersion.Value;

            var docRoot = $"/home/{user.Username}/files/{environment.Name}{(sw_version[0] == '6' ? "/public" : "")}";
            var logRoot = $"/home/{user.Username}/files/logs/{environment.Name}";

            var conf = ApacheConfConstructor.Construct
                .WithVersion(version)
                .WithEmail(user.Email)
                .WithAddress(environment.Address)
                .WithDocRoot(docRoot)
                .WithLogRoot(logRoot)
                .WithSSLCertFile(DB.Settings.Get("SSLCertificateFile").Value)
                .WithSSLKeyFile(DB.Settings.Get("SSLCertificateKeyFile").Value)
                .WithSSLChainFile(DB.Settings.Get("SSLCertificateChainFile").Value)
                .WithUsername(user.Username).BuildSSL();

            File.WriteAllText($"/etc/apache2/sites-available/{user.Username}_{environment.Name}.conf", conf);
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service apache2 reload\"")
                .ExecuteAsync();
            var cmd = await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"a2ensite {user.Username}_{environment.Name}.conf\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service apache2 reload\"")
                .ExecuteAsync();
        }

        public void SetTaskRunning(long id, bool running)
        {
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand($"UPDATE environments_settings_values SET `Value` = '{running}' WHERE environments_ID_fk = @envid And environments_settings_ID_fk = 4;");
                Command.Parameters.AddWithValue("@envid", id);
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }
        }

        public void IncreaseSorting(long id)
        {
            var env = Get(id);
            using var connection = DB.GetConnection();
            connection.Execute("UPDATE `environments` SET `Sorting` = @sorting WHERE `ID` = @id;", new
            {
                id = id,
                sorting = env.Sorting + 1
            });
        }

        public void DecreaseSorting(long id)
        {
            var env = Get(id);
            using var connection = DB.GetConnection();
            connection.Execute("UPDATE `environments` SET `Sorting` = @sorting WHERE `ID` = @id;", new
            {
                id = id,
                sorting = env.Sorting - 1
            });
        }

        public async Task DeleteAsync(Environment environment, User user)
        {
            DB.Logs.Add("DAL", "Delete Environment " + environment.Name + " for " + user.Username);

            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"a2dissite {user.Username}_{environment.Name}.conf\"")
                .ExecuteAsync();
            await Cli.Wrap("/bin/bash")
                .WithArguments("-c \"service apache2 reload\"")
                .ExecuteAsync();

            File.Delete($"/etc/apache2/sites-available/{user.Username}_{environment.Name}.conf");

            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("DELETE FROM `environments` WHERE `environments`.`ID` = @id");
                Command.Parameters.AddWithValue("@id", environment.ID);
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }

            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("DELETE FROM `environments_snapshots` WHERE `environments_snapshots`.`environments_Id_fk` = @id");
                Command.Parameters.AddWithValue("@id", environment.ID);
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }
            var dbString = user.Username + "_" + environment.Name;
            using (var connection = DB.GetConnection())
            {
                var Command = new MySqlCommand("DROP DATABASE " + dbString + ";");
                Command.Connection = connection;
                Command.ExecuteNonQuery();
            }


            using (var connection = DB.GetConnection())
                connection.Execute($"drop user {MySqlHelper.EscapeString(dbString)}@'localhost';");

            Directory.Delete($"/home/{user.Username}/files/{environment.Name}", true);
            Directory.Delete($"/home/{user.Username}/files/logs/{environment.Name}", true);
        }

        public string FixEnvironmentName(string name) => name.ToLower().Replace(" ", "_").Replace(".", "_").Replace("-", "_");
    }
}
