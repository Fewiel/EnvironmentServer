using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions
{
    internal class EnvironmentDelete : ActionBase
    {
        public override string ActionIdentifier => "delete_environment";

        public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
        {
            var db = sp.GetService<Database>();
            var env = db.Environments.Get(variableID);
            var usr = db.Users.GetByID(userID);

            await db.Environments.DeleteAsync(env, usr).ConfigureAwait(false);
        }
    }
}
