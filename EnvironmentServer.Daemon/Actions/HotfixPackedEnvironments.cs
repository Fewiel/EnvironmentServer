using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions
{
    internal class HotfixPackedEnvironments : ActionBase
    {
        public override string ActionIdentifier => "deploy_hotfix";

        public override Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
        {
            var db = sp.GetService<Database>();

            foreach (var env in db.Environments.GetAll())
            {
                if (!env.Stored)
                    continue;

                var usr = db.Users.GetByID(env.UserID);

                if (!File.Exists($"/home/{usr.Username}/files/{env.InternalName}/public/index.html"))
                {
                    var content = $@"<!DOCTYPE html>
                            <html>
                                <head>
                                    <meta http-equiv=""Refresh"" content=""0; url=https://cp.{db.Settings.Get("domain").Value}/Recover/{env.ID}"" />
                                </head> 
                            </html>";
                    Directory.CreateDirectory($"/home/{usr.Username}/files/{env.InternalName}/public");
                    Directory.CreateDirectory($"/home/{usr.Username}/files/{env.InternalName}/public/admin");
                    File.WriteAllText($"/home/{usr.Username}/files/{env.InternalName}/public/index.html",
                        content);
                    File.WriteAllText($"/home/{usr.Username}/files/{env.InternalName}/public/admin/index.html",
                        content);

                    db.Logs.Add("Hotfix", $"Fix Environment: {env.ID} - {env.InternalName} - Used: {env.LatestUse}");
                }
            }

            return Task.CompletedTask;
        }
    }
}
