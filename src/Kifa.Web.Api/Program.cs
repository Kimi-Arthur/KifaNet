using Kifa.Apps.MomentCounter;
using Kifa.Cloud.Swisscom;
using Kifa.Configs;
using Kifa.Languages.Cambridge;
using Kifa.Languages.Dwds;
using Kifa.Languages.German;
using Kifa.Memrise;
using Kifa.Web.Api.Controllers.Accounts;
using Kifa.Web.Api.Controllers.Cambridge;
using Kifa.Web.Api.Controllers.German;
using Kifa.Web.Api.Controllers.Goethe;
using Kifa.Web.Api.Controllers.MomentCounter;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using NLog.Web;

namespace Kifa.Web.Api;

public class Program {
    public static void Main(string[] args) {
        KifaConfigs.Init();
        RegisterClients();
        Logging.ConfigureLogger();
        KifaConfigs.LoggerConfigured();

        CreateWebHostBuilder(args).Build().Run();
    }

    static void RegisterClients() {
        Counter.Client = new CounterJsonServiceClient();
        SwisscomAccount.Client = new SwisscomAccountJsonServiceClient();
        GermanWord.Client = new GermanWordJsonServiceClient();
        MemriseCourse.Client = new MemriseCourseJsonServiceClient();
        MemriseWord.Client = new MemriseWordJsonServiceClient();
        CambridgePage.Client = new CambridgePageJsonServiceClient();
        CambridgeGlobalGermanWord.Client =
            new CambridgeGlobalGermanWordsController.JsonServiceClient();
        DwdsPage.Client = new DwdsPageJsonServiceClient();
        DwdsGermanWord.Client = new DwdsGermanWordJsonServiceClient();
    }

    public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        => WebHost.CreateDefaultBuilder(args).UseNLog().UseStartup<Startup>();
}
