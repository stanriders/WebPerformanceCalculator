using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace WebPerformanceCalculator
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IConfiguration Configuration { get; private set; } = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .UseSerilog((_, services, configuration) => configuration
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                    .MinimumLevel.Override("Default", LogEventLevel.Debug)
                    .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", "WebPerformanceCalculator-Sotarks")
                    .Enrich.WithClientIp("CF-Connecting-IP")
                    .Enrich.WithRequestHeader("CF-IPCountry")
                    .Enrich.WithRequestHeader("Referer")
                    .Enrich.WithRequestHeader("User-Agent")
                    .WriteTo.Console()
                    .WriteTo.Seq("http://seq:5341")
                    .ReadFrom.Services(services))
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseConfiguration(Configuration).UseStartup<Startup>(); });
        }
    }
}
