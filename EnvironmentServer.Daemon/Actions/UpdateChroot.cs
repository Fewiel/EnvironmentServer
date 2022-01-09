using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
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

        public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
        {
            var db = sp.GetService<Database>();

            foreach (var u in db.Users.GetUsers())
            {
                db.Logs.Add("Daemon", "Update Chroot for " + u.Username);
                await DAL.Repositories.UsersRepository.UpdateChrootForUserAsync(u.Username);
            }
        }
    }
}
