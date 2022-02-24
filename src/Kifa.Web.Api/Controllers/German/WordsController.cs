using Kifa.Languages.German;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers; 

[Route("api/" + GermanWord.ModelId)]
public class WordsController : KifaDataController<GermanWord, GermanWordJsonServiceClient> {
    protected override bool ShouldAutoRefresh => false;
}

public class GermanWordJsonServiceClient : KifaServiceJsonClient<GermanWord>, GermanWordServiceClient {
}