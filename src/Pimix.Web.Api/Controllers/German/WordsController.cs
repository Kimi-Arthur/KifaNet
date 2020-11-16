using Microsoft.AspNetCore.Mvc;
using Pimix.Languages.German;

namespace Pimix.Web.Api.Controllers {
    [Route("api/" + Word.ModelId)]
    public class WordsController : PimixController<Word, WordJsonServiceClient> {
    }

    public class WordJsonServiceClient : PimixServiceJsonClient<Word>, WordServiceClient {
    }
}
