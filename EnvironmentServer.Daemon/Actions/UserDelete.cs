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
        public override string ActionIdentifier => "user_delete";

        public override async Task ExecuteAsync(Database db, long variableID, long userID)
        {
            await db.Users.DeleteAsync(db.Users.GetByID(variableID));
        }
    }
}
