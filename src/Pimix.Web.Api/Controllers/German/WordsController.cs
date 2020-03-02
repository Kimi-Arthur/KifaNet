using Microsoft.AspNetCore.Mvc;
using Pimix.Languages.German;
using Pimix.Service;

namespace Pimix.Web.Api.Controllers {
    [Route("api/" + Word.ModelId)]
    public class WordsController : PimixController<Word> {
        static readonly WordServiceClient client = new WordJsonServiceClient();

        protected override PimixServiceClient<Word> Client => client;
    }

    public class WordJsonServiceClient : PimixServiceJsonClient<Word>,
        WordServiceClient {
    }
}
