using Kifa.Memrise;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Goethe;

[Route("api/" + MemriseWord.ModelId)]
public class
    MemriseWordsController : KifaDataController<MemriseWord, MemriseWordJsonServiceClient> {
}

public class MemriseWordJsonServiceClient : KifaServiceJsonClient<MemriseWord>,
    MemriseWordServiceClient {
}
