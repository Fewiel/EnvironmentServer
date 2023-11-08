using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            db.Logs.Add("EnvironmentDelete", "Delete Environment: " + env.InternalName + " UserID: " + env.UserID);

            await db.Environments.DeleteAsync(env, usr).ConfigureAwait(false);
        }
    }
}
