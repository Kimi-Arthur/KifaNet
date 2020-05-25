using Microsoft.AspNetCore.Mvc;
using Pimix.Infos;
using Pimix.Music;
using Pimix.Service;

namespace Pimix.Web.Api.Controllers {
    [Route("api/" + GuitarChord.ModelId)]
    public class GuitarChordsController : PimixController<GuitarChord> {
        static readonly GuitarChordServiceClient client = new GuitarChordJsonServiceClient();

        protected override PimixServiceClient<GuitarChord> Client => client;
    }

    public class GuitarChordJsonServiceClient : PimixServiceJsonClient<GuitarChord>, GuitarChordServiceClient {
    }
}
