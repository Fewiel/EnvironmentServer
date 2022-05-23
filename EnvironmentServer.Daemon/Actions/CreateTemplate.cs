using EnvironmentServer.Daemon.Utility;
using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions;

internal class CreateTemplate : ActionBase
{
    public override string ActionIdentifier => "create_template";

    public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
    {
        var db = sp.GetService<Database>();
        var em = sp.GetService<IExternalMessaging>();

        var tplDetails = JsonConvert.DeserializeObject<TemplateDetails>(db.CmdActionDetail.Get(variableID).JsonString);

        var env = db.Environments.Get(tplDetails.EnvironmentID);
        var usr = db.Users.GetByID(env.UserID);

        await EnvironmentPacker.CreateTemplateAsync(db, env, db.Templates.Get(tplDetails.TemplateID));

        db.Environments.SetTaskRunning(env.ID, false);
    }
}
