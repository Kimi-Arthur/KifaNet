using Kifa.Apps.MomentCounter;
using Kifa.Cloud.Google;
using Kifa.Cloud.Swisscom;
using Kifa.Configs;
using Kifa.Languages.Cambridge;
using Kifa.Languages.Dwds;
using Kifa.Languages.German;
using Kifa.Memrise;
using Kifa.Web.Api.Controllers.Goethe;
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
        Counter.Client = new KifaServiceJsonClient<Counter>();
        SwisscomAccount.Client = new KifaServiceJsonClient<SwisscomAccount>();
        GermanWord.Client = new KifaServiceJsonClient<GermanWord>();
        MemriseCourse.Client = new MemriseCourseJsonServiceClient();
        MemriseWord.Client = new KifaServiceJsonClient<MemriseWord>();
        CambridgePage.Client = new KifaServiceJsonClient<CambridgePage>();
        CambridgeGlobalGermanWord.Client = new KifaServiceJsonClient<CambridgeGlobalGermanWord>();
        DwdsPage.Client = new KifaServiceJsonClient<DwdsPage>();
        DwdsGermanWord.Client = new KifaServiceJsonClient<DwdsGermanWord>();
        GoogleAccount.Client = new KifaServiceJsonClient<GoogleAccount>();
    }

    public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        => WebHost.CreateDefaultBuilder(args).UseNLog().UseStartup<Startup>();
}
