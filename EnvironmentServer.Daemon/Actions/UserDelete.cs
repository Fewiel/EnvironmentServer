using EnvironmentServer.DAL;
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

        public override async Task ExecuteAsync(Database db, long variableID, long userID)
        {
            db.Logs.Add("Daemon", "Delete User: " + db.Users.GetByID(variableID).Username + " - ID: " + db.Users.GetByID(variableID).ID);
            await db.Users.DeleteAsync(db.Users.GetByID(variableID));
            db.Logs.Add("Daemon", "Delete User done: " + db.Users.GetByID(variableID).Username + " - ID: " + db.Users.GetByID(variableID).ID);
        }
    }
}
