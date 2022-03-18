using CliWrap;
using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.ScheduleActions;

internal class PackEnvironments : ScheduledActionBase
{
    public PackEnvironments(ServiceProvider sp) : base(sp)
    {
    }

    public override string ActionIdentifier => "pack_environments";

    public override async Task ExecuteAsync(Database db)
    {
        var environments = db.Environments.GetAllUnstored();

        foreach (var env in environments)
        {
            try
            {

                if (env.LatestUse.AddDays(7) < DateTime.Now)
                {
                    var usr = db.Users.GetByID(env.UserID);

                    var sw6 = Directory.Exists($"/home/{usr.Username}/files/{env.Name}/public");

                    db.Logs.Add("Daemon", $"Packing Environment: {env.Name} User: {db.Users.GetByID(env.UserID).Username}");

                    foreach (var f in Directory.GetDirectories($"/home/{usr.Username}/files/{env.Name}/var/cache"))
                    {
                        if (f.Contains("prod"))
                            Directory.Delete(f, true);
                    }

                    Directory.CreateDirectory($"/home/{usr.Username}/files/inactive");

                    if (File.Exists($"/home/{usr.Username}/files/inactive/{env.Name}.zip"))
                        File.Delete($"/home/{usr.Username}/files/inactive/{env.Name}.zip");

                    await Cli.Wrap("/bin/bash")
                        .WithArguments($"-c \"zip -r /home/{usr.Username}/files/inactive/{env.Name}.zip {env.Name}\"")
                        .WithWorkingDirectory($"/home/{usr.Username}/files/")
                        .ExecuteAsync();

                    Directory.Delete($"/home/{usr.Username}/files/{env.Name}", true);
                    Directory.CreateDirectory($"/home/{usr.Username}/files/{env.Name}");

                    var content = $@"<!DOCTYPE html>
                            <html>
                                <head>
                                    <meta http-equiv=""Refresh"" content=""0; url=https://cp.{db.Settings.Get("domain").Value}/Recover/{env.ID}"" />
                                </head> 
                            </html>";

                    if (sw6)
                    {
                        Directory.CreateDirectory($"/home/{usr.Username}/files/{env.Name}/public");
                        Directory.CreateDirectory($"/home/{usr.Username}/files/{env.Name}/public/admin");
                    }
                    else
                    {
                        Directory.CreateDirectory($"/home/{usr.Username}/files/{env.Name}/backend");
                    }

                    File.WriteAllText($"/home/{usr.Username}/files/{env.Name}/{(sw6 ? "public/" : "")}index.html",
                            content);

                    File.WriteAllText($"/home/{usr.Username}/files/{env.Name}/{(sw6 ? "public/admin" : "backend/")}index.html",
                            content);

                    db.Environments.SetStored(env.ID, true);

                    await Cli.Wrap("/bin/bash")
                        .WithArguments($"-c \"chown -R {usr.Username}:sftp_users /home/{usr.Username}/files/{env.Name}\"")
                        .ExecuteAsync();

                    db.Logs.Add("Daemon", $"Packing complete for Environment: {env.Name} User: {db.Users.GetByID(env.UserID).Username}");
                }
            }
            catch (Exception ex)
            {
                db.Logs.Add("PackEnvironments", ex.ToString());
            }
        }
    }
}
