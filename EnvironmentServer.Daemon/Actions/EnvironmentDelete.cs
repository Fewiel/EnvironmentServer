using EnvironmentServer.DAL;
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

        public override async Task ExecuteAsync(Database db, long variableID, long userID)
        {
            var env = db.Environments.Get(variableID);
            var usr = db.Users.GetByID(userID);

            await db.Environments.DeleteAsync(env, usr).ConfigureAwait(false);
        }
    }
}
