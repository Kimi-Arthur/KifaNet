using System;
using Kifa.Apps.MomentCounter;
using Kifa.Cloud.Swisscom;
using Kifa.Configs;
using Kifa.Languages.German;
using Kifa.Web.Api.Controllers;
using Kifa.Web.Api.Controllers.Accounts;
using Kifa.Web.Api.Controllers.MomentCounter;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using NLog;

namespace Kifa.Web.Api;

public class Program {
    public static void Main(string[] args) {
        AppDomain.CurrentDomain.AssemblyLoad += (sender, eventArgs)
            => KifaConfigs.LoadFromSystemConfigs(eventArgs.LoadedAssembly);

        KifaConfigs.LoadFromSystemConfigs();
        RegisterClients();
        ConfigureLogger();

        CreateWebHostBuilder(args).Build().Run();
    }

    static void ConfigureLogger() {
        LogManager.Configuration.LoggingRules.Clear();

        LogManager.Configuration.AddRule(LogLevel.Debug, LogLevel.Fatal, "console");
        LogManager.Configuration.AddRule(LogLevel.Trace, LogLevel.Fatal, "file_full");

        LogManager.ReconfigExistingLoggers();
    }

    static void RegisterClients() {
        Counter.Client = new CounterJsonServiceClient();
        SwisscomAccountQuota.AccountClient = new SwisscomAccountJsonServiceClient();
        DwdsClient.GermanWordClient = new GermanWordJsonServiceClient();
    }

    public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        => WebHost.CreateDefaultBuilder(args).UseStartup<Startup>();
}
