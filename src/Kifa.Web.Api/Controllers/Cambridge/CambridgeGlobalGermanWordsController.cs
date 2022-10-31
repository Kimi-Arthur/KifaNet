using Kifa.Languages.Cambridge;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Cambridge;

[Route("api/" + CambridgeGlobalGermanWord.ModelId)]
public class CambridgeGlobalGermanWordsController : KifaDataController<CambridgeGlobalGermanWord,
    CambridgeGlobalGermanWordJsonServiceClient> {
}

public class CambridgeGlobalGermanWordJsonServiceClient :
    KifaServiceJsonClient<CambridgeGlobalGermanWord>, CambridgeGlobalGermanWordServiceClient {
}
