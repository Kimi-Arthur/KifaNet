using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using MongoDB.Bson.Serialization.Conventions;
using Pimix.Configs;

namespace Pimix.Web.Api {
    public class Program {
        public static void Main(string[] args) {
            AppDomain.CurrentDomain.AssemblyLoad +=
                (sender, eventArgs) => PimixConfigs.LoadFromSystemConfigs(eventArgs.LoadedAssembly);

            PimixConfigs.LoadFromSystemConfigs();

            ConventionRegistry.Register("Correct Conventions", new ConventionPack {
                    new SnakeNameConvention()
                },
                _ => true);


            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
            => WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
