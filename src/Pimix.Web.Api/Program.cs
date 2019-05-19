using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Pimix.Configs;

namespace Pimix.Web.Api {
    public class Program {
        public static void Main(string[] args) {
            AppDomain.CurrentDomain.AssemblyLoad +=
                (sender, eventArgs) => PimixConfigs.LoadFromSystemConfigs(eventArgs.LoadedAssembly);

            PimixConfigs.LoadFromSystemConfigs();

            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
            => WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
