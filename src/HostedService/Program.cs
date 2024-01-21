using Microsoft.AspNetCore;

namespace HostedService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .ConfigureLogging(l => l.ClearProviders())
            .UseDefaultServiceProvider(opt => opt.ValidateScopes = false); 
    }
}