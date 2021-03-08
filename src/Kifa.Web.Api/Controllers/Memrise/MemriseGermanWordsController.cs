using Kifa.Languages.German.Goethe;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Memrise {
    [Route("api/" + GoetheGermanWord.ModelId)]
    public class
        MemriseGermanWordsController : KifaDataController<GoetheGermanWord, MemriseGermanWordJsonServiceClient> {
        protected override bool ShouldAutoRefresh => false;
    }

    public class MemriseGermanWordJsonServiceClient : KifaServiceJsonClient<GoetheGermanWord>,
        MemriseGermanWordServiceClient {
    }
}
