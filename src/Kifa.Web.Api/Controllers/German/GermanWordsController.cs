using Kifa.Languages.German;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.German;

public class GermanWordsController : KifaDataController<GermanWord, GermanWordJsonServiceClient> {
}

public class GermanWordJsonServiceClient : KifaServiceJsonClient<GermanWord>,
    GermanWordServiceClient {
}
