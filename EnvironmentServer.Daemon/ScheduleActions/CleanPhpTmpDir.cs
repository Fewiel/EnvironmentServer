using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.ScheduleActions
{
    internal class CleanPhpTmpDir : ScheduledActionBase
    {
        public CleanPhpTmpDir(ServiceProvider sp) : base(sp)
        {
        }

        public override string ActionIdentifier => "cleanup_php_tmp_dir";

        public override Task ExecuteAsync(Database db)
        {
            var users = db.Users.GetUsers();

            foreach (var usr in users)
            {
                Console.WriteLine("Delete tmp for " + usr.Username);
                var path = $"/home/{usr.Username}/files/php/tmp";
                if (!Directory.Exists(path))
                    continue;
                foreach (var f in Directory.GetFiles(path))
                {
                    if (File.GetCreationTime(f).AddDays(1) <= DateTime.Now)
                        File.Delete(f);
                }
            }

            return Task.CompletedTask;
        }
    }
}
