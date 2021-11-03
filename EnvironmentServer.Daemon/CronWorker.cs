using EnvironmentServer.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon
{
    public class CronWorker
    {
        private Database DB;
        private readonly CancellationTokenSource cancellationToken;
        private readonly Task ActiveWorkerTask;

        public CronWorker(Database db)
        {
            DB = db;
            cancellationToken = new CancellationTokenSource();
            ActiveWorkerTask = Task.Factory.StartNew(DoWork, cancellationToken.Token);
        }

        public void StopWorker()
        {
            cancellationToken.Cancel();
            Console.WriteLine("Waiting for last cron");
            ActiveWorkerTask.Wait(TimeSpan.FromMinutes(15));
        }

        private async Task DoWork()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
            }
        }
    }
}
