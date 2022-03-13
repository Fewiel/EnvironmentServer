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
        var environments = db.Environments.GetAll();

        foreach (var env in environments)
        {
            if (env.LatestUse.AddDays(14) < DateTime.Now)
            {
                var usr = db.Users.GetByID(env.UserID);

                if (File.Exists($"/home/{usr.Username}/files/{env.Name}/.stored"))
                    return;

                var sw6 = Directory.Exists($"/home/{usr.Username}/files/{env.Name}/public");

                db.Logs.Add("Daemon", $"Packing Environment: {env.Name} User: {db.Users.GetByID(env.UserID).Username}");

                foreach (var f in Directory.GetDirectories($"/home/{usr.Username}/files/{env.Name}/var/cache"))
                {
                    if (f.Contains("prod"))
                        Directory.Delete(f, true);
                }

                Directory.CreateDirectory($"/home/{usr.Username}/files/inactive");

                await Cli.Wrap("/bin/bash")
                    .WithArguments($"-c \"tar -czvf /home/{usr.Username}/files/inactive/{env.Name}.tar.gz /home/{usr.Username}/files/{env.Name}\"")
                    .WithWorkingDirectory($"/home/{usr.Username}/files/{env.Name}")
                    .ExecuteAsync();

                Directory.Delete($"/home/{usr.Username}/files/{env.Name}", true);
                Directory.CreateDirectory($"/home/{usr.Username}/files/{env.Name}");

                if (sw6)
                {
                    File.WriteAllText($"/home/{usr.Username}/files/{env.Name}/public/index.html",
                        "<!DOCTYPE html>" + Environment.NewLine +
                        "   <html>" + Environment.NewLine +
                        "       <head>" + Environment.NewLine +
                        $"           <meta http-equiv=\"Refresh\" content=\"0; url=https://cp.{db.Settings.Get("domain")}/recover/{env.ID}\" />" + Environment.NewLine +
                        "       </head>" + Environment.NewLine +
                        "   </html>");
                }
                else
                {
                    File.WriteAllText($"/home/{usr.Username}/files/{env.Name}/index.html",
                        "<!DOCTYPE html>" + Environment.NewLine +
                        "   <html>" + Environment.NewLine +
                        "       <head>" + Environment.NewLine +
                        $"           <meta http-equiv=\"Refresh\" content=\"0; url=https://cp.{db.Settings.Get("domain")}/recover/{env.ID}\" />" + Environment.NewLine +
                        "       </head>" + Environment.NewLine +
                        "   </html>");
                }

                await Cli.Wrap("/bin/bash")
                    .WithArguments($"-c \"chown -R {usr.Username} /home/{usr.Username}/files/{env.Name}\"")
                    .ExecuteAsync();
            }
        }
    }
}
