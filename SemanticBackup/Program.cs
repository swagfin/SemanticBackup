using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Reflection;

namespace SemanticBackup
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseConfiguration(new ConfigurationBuilder()
                       .SetBasePath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs"))
                       .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                       .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true, reloadOnChange: true)
                       .Build());
                    webBuilder.UseStartup<Startup>();
                });

        public static string AppVersion
        {
            get
            {
#if DEBUG
                return string.Format("v.{0} (Developer Edition)", Assembly.GetExecutingAssembly().GetName().Version.ToString());
#else
                return string.Format("v.{0}", Assembly.GetExecutingAssembly().GetName().Version.ToString());
#endif
            }
        }
    }
}
