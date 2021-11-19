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
            var hc = new HttpClient();
            hc.DefaultRequestHeaders.Add("User-Agent", "Environment Server - Daemon");
            var sp = new ServiceCollection()
                .AddSingleton(hc)
                .AddSingleton(new Database($"server={Config.Host};database={Config.Database};uid={Config.Username};pwd={Config.Password};"))
                .BuildServiceProvider();

            var w = new Worker(sp);
            var cw = new CronWorker(sp);
            string cmd = "";
            while (cmd != "1")
            {
                Console.WriteLine("Enter 1 to stop deamon");
                cmd = Console.ReadLine();                    
            }

            w.StopWorker();
            cw.StopWorker();
        }
    }
}
