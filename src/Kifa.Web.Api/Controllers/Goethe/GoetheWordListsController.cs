using Kifa.Languages.German.Goethe;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Goethe;

[Route("api/" + GoetheWordList.ModelId)]
public class GoetheWordListsController : KifaDataController<GoetheWordList,
    GoetheWordListJsonServiceClient> {
}

public class GoetheWordListJsonServiceClient : KifaServiceJsonClient<GoetheWordList>,
    GoetheWordListServiceClient {
}
