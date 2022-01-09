using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions
{
    internal class UserDelete : ActionBase
    {
        public override string ActionIdentifier => "delete_user";

        public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
        {
            var db = sp.GetService<Database>();

            db.Logs.Add("Daemon", "Delete User: " + db.Users.GetByID(variableID).Username + " - ID: " + db.Users.GetByID(variableID).ID);
            await db.Users.DeleteAsync(db.Users.GetByID(variableID));
        }
    }
}
