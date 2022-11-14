using Kifa.Languages.Dwds;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.German;

[Route("api/" + DwdsGermanWord.ModelId)]
public class
    DwdsGermanWordsController : KifaDataController<DwdsGermanWord,
        DwdsGermanWordJsonServiceClient> {
}

public class DwdsGermanWordJsonServiceClient : KifaServiceJsonClient<DwdsGermanWord>,
    DwdsGermanWord.ServiceClient {
}
