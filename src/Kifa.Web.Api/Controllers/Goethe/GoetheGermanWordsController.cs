using Kifa.Languages.German.Goethe;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Goethe {
    [Route("api/" + GoetheGermanWord.ModelId)]
    public class GoetheGermanWordsController : KifaDataController<GoetheGermanWord, GoetheGermanWordJsonServiceClient> {
    }

    public class GoetheGermanWordJsonServiceClient : KifaServiceJsonClient<GoetheGermanWord>,
        GoetheGermanWordServiceClient {
    }
}
