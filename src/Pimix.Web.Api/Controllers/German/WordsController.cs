using Microsoft.AspNetCore.Mvc;
using Pimix.Languages.German;

namespace Pimix.Web.Api.Controllers {
    [Route("api/" + Word.ModelId)]
    public class WordsController : KifaDataController<Word, WordJsonServiceClient> {
        protected override bool ShouldAutoRefresh => false;
    }

    public class WordJsonServiceClient : KifaServiceJsonClient<Word>, WordServiceClient {
    }
}
