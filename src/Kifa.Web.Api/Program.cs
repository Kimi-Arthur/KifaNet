using System;
using Kifa.Configs;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Kifa.Web.Api {
    public class Program {
        public static void Main(string[] args) {
            AppDomain.CurrentDomain.AssemblyLoad += (sender, eventArgs) =>
                KifaConfigs.LoadFromSystemConfigs(eventArgs.LoadedAssembly);

            KifaConfigs.LoadFromSystemConfigs();

            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args).UseStartup<Startup>();
    }
}
