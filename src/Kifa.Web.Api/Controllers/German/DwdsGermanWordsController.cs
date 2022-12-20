using Kifa.Languages.Dwds;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.German;


public class
    DwdsGermanWordsController : KifaDataController<DwdsGermanWord,
        DwdsGermanWordJsonServiceClient> {
}

public class DwdsGermanWordJsonServiceClient : KifaServiceJsonClient<DwdsGermanWord>,
    DwdsGermanWord.ServiceClient {
}
