using Microsoft.AspNetCore.Mvc;
using Pimix.Music;

namespace Pimix.Web.Api.Controllers {
    [Route("api/" + GuitarChord.ModelId)]
    public class GuitarChordsController : KifaDataController<GuitarChord, GuitarChordJsonServiceClient> {
    }

    public class GuitarChordJsonServiceClient : PimixServiceJsonClient<GuitarChord>, GuitarChordServiceClient {
    }
}
