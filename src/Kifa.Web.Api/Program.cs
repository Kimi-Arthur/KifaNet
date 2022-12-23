using Kifa.Configs;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using NLog.Web;

namespace Kifa.Web.Api;

public class Program {
    public static void Main(string[] args) {
        KifaConfigs.Init();
        Logging.ConfigureLogger();
        KifaConfigs.LoggerConfigured();
        Assemblies.LoadAll();

        CreateWebHostBuilder(args).Build().Run();
    }

    public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        => WebHost.CreateDefaultBuilder(args).UseNLog().UseStartup<Startup>();
}
