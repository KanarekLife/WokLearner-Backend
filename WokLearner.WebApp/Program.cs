using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace WokLearner.WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (!File.Exists("appsettings.json"))
            {
                File.Copy("sample-appsettings.json", "appsettings.json");
            }
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
        }
    }
}