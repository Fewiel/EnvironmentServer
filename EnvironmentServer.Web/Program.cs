using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace EnvironmentServer.Web;

public class Program
{
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        CreateHostBuilder(args).Build().Run();
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        File.AppendAllText($"error_log_web_{DateTime.Now:dd_MM_yyyy}.log", e.ExceptionObject.ToString());
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
#if DEBUG
                    webBuilder.UseUrls("http://0.0.0.0:5000");
#else
                    webBuilder.UseUrls("http://0.0.0.0:5000");
#endif
                    webBuilder.UseStartup<Startup>();
            });
}
