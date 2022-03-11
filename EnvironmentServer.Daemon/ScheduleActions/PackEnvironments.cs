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
                
                foreach (var f in Directory.GetDirectories($"/home/{usr.Username}/files/{env.Name}/var/cache"))
                {
                    if (f.Contains("prod"))
                        Directory.Delete(f, true);
                }

                Directory.CreateDirectory("/home/{usr.Username}/files/inactive");
                
                await Cli.Wrap("/bin/bash")
                    .WithArguments($"-c \"tar -czvf /home/{usr.Username}/files/inactive/{env.Name}.tar.gz /home/{usr.Username}/files/{env.Name}/*\"")
                    .WithWorkingDirectory($"/home/{usr.Username}/files/{env.Name}")
                    .ExecuteAsync();

                Directory.Delete($"/home/{usr.Username}/files/{env.Name}", true);
                Directory.CreateDirectory($"/home/{usr.Username}/files/{env.Name}");

                await Cli.Wrap("/bin/bash")
                    .WithArguments($"-c \"chown -R {usr.Username} /home/{usr.Username}/files/{env.Name}\"")
                    .ExecuteAsync();

                File.WriteAllText($"/home/{usr.Username}/files/{env.Name}/index.html", 
                    "<!DOCTYPE html>" +
                    "   <html>" +
                    "       <head>" +
                    $"           <meta http-equiv=\"Refresh\" content=\"0; url=https://cp.{db.Settings.Get("domain")}/recover/{env.ID}\" />" +
                    "       </head>" +
                    "   </html>");
            }
        }
    }
}
