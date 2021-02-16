using Microsoft.AspNetCore.Mvc;
using Kifa.Music;

namespace Kifa.Web.Api.Controllers {
    [Route("api/" + GuitarChord.ModelId)]
    public class GuitarChordsController : KifaDataController<GuitarChord, GuitarChordJsonServiceClient> {
    }

    public class GuitarChordJsonServiceClient : KifaServiceJsonClient<GuitarChord>, GuitarChordServiceClient {
    }
}
