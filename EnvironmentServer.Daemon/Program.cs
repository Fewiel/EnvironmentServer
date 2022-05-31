using EnvironmentServer.Daemon.ScheduleActions;
using EnvironmentServer.Daemon.Services;
using EnvironmentServer.DAL;
using EnvironmentServer.Interfaces;
using EnvironmentServer.SlackBot;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon
{
    class Program
    {
        private static readonly DBConfig Config
            = JsonConvert.DeserializeObject<DBConfig>(File.ReadAllText("DBConfig.json"));

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            File.AppendAllText($"error_log_daemon_{DateTime.Now:dd_MM_yyyy}.log", e.ExceptionObject.ToString());
        }

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            var db = new Database($"server={Config.Host};database={Config.Database};" +
                $"uid={Config.Username};pwd={Config.Password};");
            var hc = new HttpClient();
            hc.DefaultRequestHeaders.Add("User-Agent", "Environment Server - Daemon");
            var sc = new ServiceCollection()
                .AddSingleton(hc)
                .AddSingleton(db);

            if (db.Settings.Get("slack_enable").Value == "1")
            {
                Console.WriteLine("SlackEnabled: " + db.Settings.Get("slack_api_key").Value);
                sc.AddSingleton<IExternalMessaging>(new Bot(db.Settings.Get("slack_api_key").Value));
            }
            else
            {
                Console.WriteLine("SlackDisabled");
                sc.AddSingleton<IExternalMessaging>(new NoExternalMessaging());
            }

            var sp = sc.BuildServiceProvider();

            var w = new Worker(sp);
            var cw = new CronWorker(sp);
            while (true)
            {
                Thread.Sleep(5000);

                if (w.ActiveWorkerTask.IsFaulted)
                {
                    db.Logs.Add("Deamon", "ERROR: Worker IsFaulted: " + w.ActiveWorkerTask.Exception.ToString());
                    w = new Worker(sp);
                }
            }
        }
    }
}