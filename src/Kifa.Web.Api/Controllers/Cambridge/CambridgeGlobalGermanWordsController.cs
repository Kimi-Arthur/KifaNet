using Kifa.Languages.Cambridge;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Cambridge;

public class CambridgeGlobalGermanWordsController : KifaDataController<CambridgeGlobalGermanWord,
    CambridgeGlobalGermanWordsController.JsonServiceClient> {
    public class JsonServiceClient : KifaServiceJsonClient<CambridgeGlobalGermanWord>,
        CambridgeGlobalGermanWord.ServiceClient {
    }
}
