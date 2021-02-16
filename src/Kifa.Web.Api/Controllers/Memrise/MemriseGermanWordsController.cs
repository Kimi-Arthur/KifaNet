using Kifa.Memrise;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Memrise {
    [Route("api/" + MemriseGermanWord.ModelId)]
    public class
        MemriseGermanWordsController : KifaDataController<MemriseGermanWord, MemriseGermanWordJsonServiceClient> {
        protected override bool ShouldAutoRefresh => false;
    }

    public class MemriseGermanWordJsonServiceClient : KifaServiceJsonClient<MemriseGermanWord>,
        MemriseGermanWordServiceClient {
    }
}
