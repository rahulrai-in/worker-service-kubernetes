using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace OfficeCountdownClock
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Register services that do actual work here.
                    services.AddHostedService<Worker>();

                    // Health check services. A custom health check service is added for demo.
                    services.AddHealthChecks().AddCheck<CustomHealthCheck>("custom_hc");
                    services.AddHostedService<TcpHealthProbeService>();
                });
        }
    }
}