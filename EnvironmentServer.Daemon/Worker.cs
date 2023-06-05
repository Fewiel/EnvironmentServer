using EnvironmentServer.Daemon.Actions;
using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Interfaces;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly ServiceProvider SP;
        private readonly CancellationTokenSource cancellationToken;
        private readonly Dictionary<string, ActionBase> Actions = new();

        public Task ActiveWorkerTask { get; }

        public Worker(ServiceProvider sp)
        {
            FillActions();
            DB = sp.GetService<Database>();
            SP = sp;
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
                File.WriteAllText("/root/logs/latest_GetFirstNonExecuted.log", DateTime.Now.ToString());
                var task = new CmdAction();
                try
                {
                    task = DB.CmdAction.GetFirstNonExecuted();
                }
                catch (Exception ex)
                {
                    //Dirty fix to gain some time
                }

                if (string.IsNullOrEmpty(task.Action))
                {
                    Thread.Sleep(500);
                    continue;
                }

                //Get correct action for task
                if (!Actions.TryGetValue(task.Action, out var act))
                {
                    DB.Logs.Add("Deamon", "Undefined action called: " + task.Action);
                    DB.CmdAction.SetExecuted(task.Id, task.Action, task.ExecutedById);
                    continue;
                }

                //Execute action
                try
                {
                    Console.WriteLine("Run Task: " + task.Action);
                    DB.Logs.Add("Deamon", $"Task started: {JsonConvert.SerializeObject(task)}");
                    File.WriteAllText("/root/logs/latest_TaskStart.log", DateTime.Now.ToString());

                    await act.ExecuteAsync(SP, task.Id_Variable, task.ExecutedById);

                    File.WriteAllText("/root/logs/latest_TaskEnd.log", DateTime.Now.ToString());
                    DB.Logs.Add("Deamon", $"Task end: {JsonConvert.SerializeObject(task)}");
                }
                catch (Exception ex)
                {
#if DEBUG
                    throw;
#endif
                    Console.WriteLine(ex.ToString());
                    File.WriteAllText("/root/logs/latest_TaskException.log", DateTime.Now.ToString());
                    DB.Logs.Add("Daemon", "ERROR in Worker: " + ex.ToString());
                }

                //Set executed in DB
                DB.CmdAction.SetExecuted(task.Id, task.Action, task.ExecutedById);
            }

            DB.Logs.Add("Deamon", "ERROR: Deamon exited DoWork");
            var em = SP.GetService<IExternalMessaging>();
            await em.SendMessageAsync("Deamon exited DoWork", "U02954V4Q6B");
        }

        private void FillActions()
        {
            var l = new List<ActionBase>
            {
                new SnapshotCreate(),
                new SnapshotRestoreLatest(),
                new SnapshotRestore(),
                new DownloadExtract(),
                new UserDelete(),
                new EnvironmentDelete(),
                new CloneGit(),
                new UpdateChroot(),
                new DownloadExtractAutoinstall(),
                new RestoreEnvironment(),
                new RegeneratePhpConfig(),
                new FastDeploy(),
                new CreateTemplate(),
                new DeleteTemplate(),
                new HotfixPackedEnvironments(),
                new EnvironmentSetDevelopment(),
                new Actions.Docker.Create(),
                new Actions.Docker.Start(),
                new Actions.Docker.Stop(),
                new Actions.Docker.StopAll(),
                new Actions.Docker.Delete(),
                new Actions.Docker.Cleanup(),
                new Actions.ShopwareConfigFiles.GetConfig(),
                new Actions.ShopwareConfigFiles.UpdateConfig(),
                new Actions.ShopwareConfigFiles.WriteConfig(),
                new BackupEnvironment(),
                new CloneProductionTemplate(),
                new CloneProductionTemplateAutoinstall(),
                new ReRunUserInstallation()
            };

            foreach (var a in l)
            {
                Actions.Add(a.ActionIdentifier, a);
            }
        }
    }
}
