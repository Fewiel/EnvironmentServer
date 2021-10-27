using EnvironmentServer.Daemon.Actions;
using EnvironmentServer.DAL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon
{
    public class Worker
    {
        private readonly Database DB;
        private readonly CancellationTokenSource cancellationToken;
        private readonly Task ActiveWorkerTask;
        private readonly Dictionary<string, ActionBase> Actions = new();
        private readonly DBConfig Config = JsonConvert.DeserializeObject<DBConfig>(File.ReadAllText("DBConfig.json"));

        public Worker()
        {
            FillActions();
            DB = new Database($"server={Config.Host};database={Config.Database};uid={Config.Username};pwd={Config.Password};");
            cancellationToken = new CancellationTokenSource();
            ActiveWorkerTask = Task.Factory.StartNew(DoWork, cancellationToken.Token);
        }

        public void StopWorker()
        {
            cancellationToken.Cancel();
            Console.WriteLine("Waiting for last task");
            ActiveWorkerTask.Wait(TimeSpan.FromMinutes(5));
        }

        private async Task DoWork()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                //Get task
                var task = DB.CmdAction.GetFirstNonExecuted();
                if (string.IsNullOrEmpty(task.Action))
                {
                    Thread.Yield();
                    continue;
                }

                //Get correct action for task
                if (!Actions.TryGetValue(task.Action, out var act))
                {
                    DB.Logs.Add("Deamon", "Undefined action called: " + task.Action);
                    DB.CmdAction.SetExecuted(task.Id);
                    continue;
                }

                //Execute action
                try
                {
                    await act.ExecuteAsync(DB, task.Id_Variable, task.ExecutedById);
                }
                catch (Exception ex)
                {
#if DEBUG
                    throw;
#endif
                    Console.WriteLine(ex.ToString());
                    DB.Logs.Add("Daemon", "ERROR in Worker: " + ex.ToString());
                }

                //Set executed in DB
                DB.CmdAction.SetExecuted(task.Id);
            }
        }

        private void FillActions()
        {
            var l = new List<ActionBase>
            {
                new SnapshotCreate(),
                new SnapshotRestoreLatest(),
                new SnapshotRestore(),
                new DownloadExtract(),
                new UserDelete()
            };

            foreach (var a in l)
            {
                Actions.Add(a.ActionIdentifier, a);
            }
        }
    }
}
