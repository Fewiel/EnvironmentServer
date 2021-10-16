using System;

namespace EnvironmentServer.Daemon
{
    class Program
    {
        static void Main(string[] args)
        {
            var w = new Worker();
            Console.WriteLine("Press Enter to stop deamon");
            Console.ReadLine();
            w.StopWorker();
        }
    }
}
