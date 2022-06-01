using EnvironmentServer.Daemon.Utility;
using EnvironmentServer.DAL;
using EnvironmentServer.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using SlackAPI;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions;

public class FastDeploy : ActionBase
{
    public override string ActionIdentifier => "fast_deploy";

    public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
    {
        var db = sp.GetService<Database>();
        var em = sp.GetService<IExternalMessaging>();
        var env = db.Environments.Get(variableID);
        var usr = db.Users.GetByID(env.UserID);
        var tmpID = System.IO.File.ReadAllText($"/home/{usr.Username}/files/{env.InternalName}/template.txt");

        await EnvironmentPacker.DeployTemplateAsync(db, env, long.Parse(tmpID));

        if (!string.IsNullOrEmpty(usr.UserInformation.SlackID))
        {
            await em.SendMessageAsync(string.Format(db.Settings.Get("fast_deploy_finished").Value, env.InternalName),
                usr.UserInformation.SlackID);
        }

        db.Environments.SetTaskRunning(env.ID, false);
    }
}
