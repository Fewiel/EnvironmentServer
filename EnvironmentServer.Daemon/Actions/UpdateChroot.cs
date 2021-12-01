using EnvironmentServer.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions
{
    internal class UpdateChroot : ActionBase
    {
        public override string ActionIdentifier => "update_chroot";

        public override async Task ExecuteAsync(Database db, long variableID, long userID)
        {
            foreach (var u in db.Users.GetUsers())
            {
                db.Logs.Add("Daemon", "Update Chroot for " + u.Username);
                await db.Users.UpdateChrootForUserAsync(u.Username);
            }
        }
    }
}
