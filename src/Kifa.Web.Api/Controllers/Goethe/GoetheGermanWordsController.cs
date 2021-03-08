using Kifa.Languages.German.Goethe;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Goethe {
    [Route("api/" + GoetheGermanWord.ModelId)]
    public class GoetheGermanWordsController : KifaDataController<GoetheGermanWord, GoetheGermanWordJsonServiceClient> {
        protected override bool ShouldAutoRefresh => false;
    }

    public class GoetheGermanWordJsonServiceClient : KifaServiceJsonClient<GoetheGermanWord>,
        GoetheGermanWordServiceClient {
    }
}
