using System;
using Kifa.Apps.MomentCounter;
using Kifa.Configs;
using Kifa.Web.Api.Controllers.MomentCounter;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Kifa.Web.Api {
    public class Program {
        public static void Main(string[] args) {
            AppDomain.CurrentDomain.AssemblyLoad += (sender, eventArgs) =>
                KifaConfigs.LoadFromSystemConfigs(eventArgs.LoadedAssembly);

            KifaConfigs.LoadFromSystemConfigs();
            RegisterClients();

            CreateWebHostBuilder(args).Build().Run();
        }

        static void RegisterClients() {
            Counter.Client = new CounterJsonServiceClient();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args).UseStartup<Startup>();
    }
}
