using Kifa.Languages.German;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers {
    [Route("api/" + Word.ModelId)]
    public class WordsController : KifaDataController<Word, WordJsonServiceClient> {
        protected override bool ShouldAutoRefresh => false;
    }

    public class WordJsonServiceClient : KifaServiceJsonClient<Word>, WordServiceClient {
    }
}
