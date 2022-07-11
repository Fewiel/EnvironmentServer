using EnvironmentServer.Daemon.Utility;
using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions;

public class DeleteTemplate : ActionBase
{
    public override string ActionIdentifier => "delete_template";

    public override Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
    {
        var db = sp.GetService<Database>();

        EnvironmentPacker.DeleteTemplate(db, variableID);

        return Task.CompletedTask;
    }
}
