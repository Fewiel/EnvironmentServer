using CliWrap;
using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Utility;

//This class is a cheesehad
internal static class EnvironmentPacker
{
    private const string PatternSW5Username = "('username' => ')(.*)(')";
    private const string PatternSW5Password = "('password' => ')(.*)(')";
    private const string PatternSW5DBName = "('dbname' => ')(.*)(')";

    private const string PatternSW6AppURL = "(APP_URL=\")(.*)(\")";
    private const string PatternSW6DatabaseURL = "(DATABASE_URL=\")(.*)(\")";
    private const string PatternSW6ComposerHome = "(COMPOSER_HOME=\")(.*)(\")";
    private const string PatternSW6ESEnabled = "(SHOPWARE_ES_ENABLED=\")(.*)(\")";
    private const string PatternSW6ESHost = "(SHOPWARE_ES_HOSTS=\")(.*)(\")";

    public static async Task PackEnvironmentAsync(Database db, Environment env)
    {
        //Delete Cache
        var usr = db.Users.GetByID(env.UserID);
        DeleteCache(usr.Username, env.InternalName);

        //Create Inactive Dir
        Directory.CreateDirectory($"/home/{usr.Username}/files/inactive");

        //Check if Environment.zip already exists and delete
        if (File.Exists($"/home/{usr.Username}/files/inactive/{env.InternalName}.zip"))
            File.Delete($"/home/{usr.Username}/files/inactive/{env.InternalName}.zip");

        //Zip Environment to inactive folder
        await Cli.Wrap("/bin/bash")
            .WithArguments($"-c \"zip -r /home/{usr.Username}/files/inactive/{env.InternalName}.zip {env.InternalName}\"")
            .WithWorkingDirectory($"/home/{usr.Username}/files/")
            .ExecuteAsync();

        //Delete Environment
        Directory.Delete($"/home/{usr.Username}/files/{env.InternalName}", true);

        //Check for SW5 or SW6
        var sw6 = Directory.Exists($"/home/{usr.Username}/files/{env.InternalName}/public");

        //Create Redirection
        Directory.CreateDirectory($"/home/{usr.Username}/files/{env.InternalName}");

        var content = $@"<!DOCTYPE html>
                            <html>
                                <head>
                                    <meta http-equiv=""Refresh"" content=""0; url=https://cp.{db.Settings.Get("domain").Value}/Recover/{env.ID}"" />
                                </head> 
                            </html>";

        if (sw6)
        {
            Directory.CreateDirectory($"/home/{usr.Username}/files/{env.InternalName}/public");
            Directory.CreateDirectory($"/home/{usr.Username}/files/{env.InternalName}/public/admin");
        }
        else
        {
            Directory.CreateDirectory($"/home/{usr.Username}/files/{env.InternalName}/backend");
        }

        File.WriteAllText($"/home/{usr.Username}/files/{env.InternalName}/{(sw6 ? "public/" : "")}index.html",
                content);

        File.WriteAllText($"/home/{usr.Username}/files/{env.InternalName}/{(sw6 ? "public/admin/" : "backend/")}index.html",
                content);

        //Set Stored in DB
        db.Environments.SetStored(env.ID, true);

        //Set Privileges to user
        await Cli.Wrap("/bin/bash")
                        .WithArguments($"-c \"chown -R {usr.Username}:sftp_users /home/{usr.Username}/files/{env.InternalName}\"")
                        .ExecuteAsync();
    }

    public static async Task UnpackEnvironmentAsync(ServiceProvider sp, Environment env)
    {
        var db = sp.GetService<Database>();
        var em = sp.GetService<IExternalMessaging>();
        var usr = db.Users.GetByID(env.UserID);

        //Check if Stored Environment exists
        if (!File.Exists($"/home/{usr.Username}/files/inactive/{env.InternalName}.zip"))
        {
            db.Logs.Add("Daemon", $"Restore Failed! File not found: /home/{usr.Username}/files/inactive/{env.InternalName}.zip");
            await em.SendMessageAsync($"Restore of Environment Failed! File not found: /home/{usr.Username}/files/inactive/{env.InternalName}.zip",
                db.UserInformation.Get(usr.ID).SlackID);
            return;
        }

        //Delete Redirection
        Directory.Delete($"/home/{usr.Username}/files/{env.InternalName}", true);

        //Unzip Environment
        await Cli.Wrap("/bin/bash")
            .WithArguments($"-c \"unzip /home/{usr.Username}/files/inactive/{env.InternalName}.zip\"")
            .WithWorkingDirectory($"/home/{usr.Username}/files")
            .ExecuteAsync();

        //Set Privileges to user
        await Cli.Wrap("/bin/bash")
            .WithArguments($"-c \"chown -R {usr.Username} /home/{usr.Username}/files/{env.InternalName}\"")
            .ExecuteAsync();

        //Delete stored environment
        File.Delete($"/home/{usr.Username}/files/inactive/{env.InternalName}.zip");

        //Set Stored false in DB
        db.Environments.SetStored(env.ID, false);
    }

    public static async Task CreateTemplateAsync(Database db, Environment env, string templateName)
    {
        var usr = db.Users.GetByID(env.UserID);

        //Disable Site
        await Cli.Wrap("/bin/bash")
            .WithArguments($"-c \"a2dissite {usr.Username}_{env.InternalName}.conf\"")
            .ExecuteAsync();
        await Cli.Wrap("/bin/bash")
            .WithArguments("-c \"service apache2 reload\"")
            .ExecuteAsync();

        //Delete Cache
        DeleteCache(usr.Username, env.InternalName);

        //Dump DB to folder
        var dbString = usr.Username + "_" + env.InternalName;

        await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"mysqldump -u {dbString} -p{env.DBPassword} " + dbString + " > db.sql\"")
                .WithWorkingDirectory($"/home/{usr.Username}/files/{env.InternalName}")
                .ExecuteAsync();

        //Move Environment to tmp folder
        var templatePath = $"/root/templates/{usr.Username}/{templateName}";
        Directory.CreateDirectory(templatePath);

        var tmpPath = $"/root/templates/tmp/{usr.Username}/{templateName}";
        Directory.CreateDirectory(tmpPath);
        
        await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"cp {env.InternalName} {tmpPath}")
                .WithWorkingDirectory($"/home/{usr.Username}/files/")
                .ExecuteAsync();

        //Enable Site
        await Cli.Wrap("/bin/bash")
            .WithArguments($"-c \"a2ensite {usr.Username}_{env.InternalName}.conf\"")
            .ExecuteAsync();
        await Cli.Wrap("/bin/bash")
            .WithArguments("-c \"service apache2 reload\"")
            .ExecuteAsync();

        //Replace parts in Config
        var sw6 = Directory.Exists($"{tmpPath}/public");

        if (sw6)
        {
            var cnf = File.ReadAllText($"{tmpPath}/{templateName}/.env");
            cnf = Regex.Replace(cnf, PatternSW6AppURL, "$1{{APPURL}}$3");
            cnf = Regex.Replace(cnf, PatternSW6ESHost, "$1$3");
            cnf = Regex.Replace(cnf, PatternSW6ESEnabled, "SHOPWARE_ES_ENABLED=\"0\"");
            cnf = Regex.Replace(cnf, PatternSW6DatabaseURL, "$1{{DATABASEURL}}$3");
            File.WriteAllText($"{tmpPath}/{templateName}/.env", cnf);
        }
        else
        {
            var cnf = File.ReadAllText($"{tmpPath}/{templateName}/config.php");
            cnf = Regex.Replace(cnf, PatternSW5Username, "$1{{DBUSER}}$2");
            cnf = Regex.Replace(cnf, PatternSW5Password, "$1{{DBPASSWORD}}$2");
            cnf = Regex.Replace(cnf, PatternSW5DBName, "$1{{DBNAME}}$2");
            File.WriteAllText($"{tmpPath}/{templateName}/config.php", cnf);
        }

        //Replace parts in DB Dump
        var dbfile = File.ReadAllText($"{tmpPath}/{templateName}/db.sql");
        dbfile = dbfile.Replace($"{env.InternalName}", "{{INTERNALNAME}}");
        dbfile = dbfile.Replace($"{usr.Username}", "{{USERNAME}}");
        File.WriteAllText(dbfile, $"{tmpPath}/{templateName}/db.sql");

        //Zip all to Template folder
        await Cli.Wrap("/bin/bash")
            .WithArguments($"-c \"zip -r {templatePath}/{templateName}.zip {tmpPath}/{templateName}\"")
            .WithWorkingDirectory($"{templatePath}")
            .ExecuteAsync();

        //Remove tmp folder
        Directory.Delete($"{tmpPath}/{templateName}", true);

        //Add Template to DB

    }

    public static void DeployTemplate()
    {
        //Create Empty Environment
        //Unzip template
        //Replace parts in config
        //Replace parts in DB Dump
        //Import DB
    }

    public static void DeleteTemplate()
    {
        //Delete Template file
        //Delete DB entry
    }

    private static void DeleteCache(string username, string environmentInternalName)
    {
        foreach (var f in Directory.GetDirectories(
            $"/home/{username}/files/{environmentInternalName}/var/cache"))
        {
            if (f.Contains("prod"))
                Directory.Delete(f, true);
        }
    }
}