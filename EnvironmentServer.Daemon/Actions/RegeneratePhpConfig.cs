using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions;

internal class RegeneratePhpConfig : ActionBase
{
    public override string ActionIdentifier => "regen_php";

    public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
    {
        var db = sp.GetService<Database>();
        await db.Users.RegenerateConfig();
    }
}
