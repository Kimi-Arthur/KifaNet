using Kifa.Languages.German.Goethe;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Goethe;


public class GoetheWordListsController : KifaDataController<GoetheWordList,
    GoetheWordListJsonServiceClient> {
}

public class GoetheWordListJsonServiceClient : KifaServiceJsonClient<GoetheWordList>,
    GoetheWordListServiceClient {
}
