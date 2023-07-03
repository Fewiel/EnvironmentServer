using EnvironmentServer.Daemon.Models;
using EnvironmentServer.DAL;
using EnvironmentServer.Interfaces;
using EnvironmentServer.Util;
using Microsoft.Extensions.DependencyInjection;
using SlackAPI;
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
        if (!System.IO.File.Exists(path))
        {
            System.IO.File.Create(path);
            await Bash.ChownAsync(user.Username, "sftp_users", path);
        }

        try
        {
            await Bash.CommandAsync($"crontab -u {user.Username} {path}");
        }
        catch (System.Exception)
        {
            if (!string.IsNullOrEmpty(user.UserInformation.SlackID))
                await em.SendMessageAsync("errors in crontab file, can't install.", user.UserInformation.SlackID);
            return;
        }

        if (!string.IsNullOrEmpty(user.UserInformation.SlackID))
            await em.SendMessageAsync("Cronjobs syncronized!", user.UserInformation.SlackID);

    }
}