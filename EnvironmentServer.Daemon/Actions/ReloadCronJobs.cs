using EnvironmentServer.Daemon.Models;
using EnvironmentServer.DAL;
using EnvironmentServer.Interfaces;
using EnvironmentServer.Util;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions;

public class ReloadCronJobs : ActionBase
{
    public override string ActionIdentifier => "reload_cronjobs";

    public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
    {
        var db = sp.GetService<Database>();
        var em = sp.GetService<IExternalMessaging>();

        var user = db.Users.GetByID(userID);
        var path = $"/home/{user.Username}/files/cronjob.txt";
        if (!File.Exists(path))
        {
            File.Create(path);
            Bash.ChownAsync(path, user.Username);
        }

        await Bash.CommandAsync($"crontab -u {user.Username} {path}");

        if (!string.IsNullOrEmpty(user.UserInformation.SlackID))
            await em.SendMessageAsync($"Cronjobs syncronized!", user.UserInformation.SlackID);
    }
}