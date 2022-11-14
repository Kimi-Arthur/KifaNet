using Kifa.Languages.German;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.German;

[Route("api/" + GermanWord.ModelId)]
public class GermanWordsController : KifaDataController<GermanWord, GermanWordJsonServiceClient> {
}

public class GermanWordJsonServiceClient : KifaServiceJsonClient<GermanWord>,
    GermanWordServiceClient {
}
