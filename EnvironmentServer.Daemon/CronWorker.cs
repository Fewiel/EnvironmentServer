using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnvironmentServer.DAL.Enums;
using System.Diagnostics.Contracts;
using EnvironmentServer.Daemon.ScheduleActions;
using Microsoft.Extensions.DependencyInjection;

namespace EnvironmentServer.Daemon
{
    public class CronWorker
    {
        private Database DB;
        private readonly CancellationTokenSource cancellationToken;
        private readonly Task ActiveWorkerTask;
        private readonly Dictionary<string, ScheduledActionBase> Actions = new();

        public CronWorker(ServiceProvider sp)
        {
            DB = sp.GetService<Database>();
            Setup();
            FillActions(sp);
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
                foreach (var sa in DB.ScheduleAction.Get(false))
                {
                    if (!ShouldExecute(sa))
                        continue;

                    DB.ScheduleAction.SetExecuted(sa.Id, LastExecutedTime(sa));

                    if (!Actions.TryGetValue(sa.Action, out var act))
                    {
                        DB.Logs.Add("Deamon", "Undefined action called: " + sa.Action);
                        continue;
                    }

                    try
                    {
                        Console.WriteLine("Run Task: " + sa.Action);
                        await act.ExecuteAsync(DB);
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        throw;
#endif
                        Console.WriteLine(ex.ToString());
                        DB.Logs.Add("Daemon", "ERROR in CronWorker: " + ex.ToString());
                    }
                }

                Thread.Sleep(50);
            }
        }

        private void Setup()
        {
            var actionList = new List<string>
            {
                "feed_refresh",
                "self_update"
            };

            foreach (var a in actionList)
            {
                DB.ScheduleAction.CreateIfNotExist(new ScheduleAction
                {
                    Action = a,
                    Interval = -1,
                    Timing = 0
                });
            }
        }

        private bool ShouldExecute(ScheduleAction a)
        {
            switch (a.Timing)
            {
                case Timing.Custom: return a.LastExecuted.AddSeconds(a.Interval) < DateTime.Now;
                case Timing.Seconds: return a.LastExecuted.AddSeconds(1) < DateTime.Now;
                case Timing.Minutes: return a.LastExecuted.AddMinutes(1) < DateTime.Now && DateTime.Now.Second >= a.Interval;
                case Timing.Hours: return a.LastExecuted.AddHours(1) < DateTime.Now && DateTime.Now.Minute >= a.Interval;
                case Timing.Days: return a.LastExecuted.AddDays(1) < DateTime.Now && DateTime.Now.Hour >= a.Interval;
                case Timing.Weeks: return a.LastExecuted.AddDays(7) < DateTime.Now && DateTime.Now.Day >= a.Interval;
                case Timing.Months: return a.LastExecuted.AddMonths(1) < DateTime.Now && DateTime.Now.Day / 7 >= a.Interval;
                case Timing.Years: return a.LastExecuted.AddYears(1) < DateTime.Now && DateTime.Now.Month >= a.Interval;
                default: return false;
            }
        }

        private DateTime LastExecutedTime(ScheduleAction a)
        {
            if (a.Timing == Timing.Custom)
                return DateTime.Now;

            var dt = DateTime.Now;
            dt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);

            if (a.Timing > Timing.Seconds)
                dt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);
            if (a.Timing > Timing.Minutes)
                dt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0);
            if (a.Timing > Timing.Hours)
                dt = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0);
            if (a.Timing > Timing.Weeks)
                dt = new DateTime(dt.Year, dt.Month, 0, 0, 0, 0);
            if (a.Timing > Timing.Months)
                dt = new DateTime(dt.Year, 0, 0, 0, 0, 0);

            return dt;
        }

        private void FillActions(ServiceProvider sp)
        {
            var l = new List<ScheduledActionBase>
            {
                new FeedRefresh(sp),
                new SelfUpdate(sp)
            };

            foreach (var a in l)
            {
                Actions.Add(a.ActionIdentifier, a);
            }
        }
    }
}
