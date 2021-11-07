using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;

namespace EnvironmentServer.Daemon
{
    class Program
    {
        private static readonly DBConfig Config = JsonConvert.DeserializeObject<DBConfig>(File.ReadAllText("DBConfig.json"));

        static void Main(string[] args)
        {
            var sp = new ServiceCollection()
                .AddSingleton(new HttpClient())
                .AddSingleton(new Database($"server={Config.Host};database={Config.Database};uid={Config.Username};pwd={Config.Password};"))
                .BuildServiceProvider();

            var w = new Worker(sp);
            var cw = new CronWorker(sp);
            Console.WriteLine("Press Enter to stop deamon");
            Console.ReadLine();
            w.StopWorker();
            cw.StopWorker();
        }
    }
}
