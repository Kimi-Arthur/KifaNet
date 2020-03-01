using Microsoft.AspNetCore.Mvc;
using Pimix.Languages.German;
using Pimix.Service;

namespace Pimix.Web.Api.Controllers {
    [Route("api/" + Verb.ModelId)]
    public class GermanVerbsController : PimixController<Verb> {
        static readonly VerbServiceClient client = new VerbJsonServiceClient();

        protected override PimixServiceClient<Verb> Client => client;
    }

    public class VerbJsonServiceClient : PimixServiceJsonClient<Verb>,
        VerbServiceClient {
    }
}
